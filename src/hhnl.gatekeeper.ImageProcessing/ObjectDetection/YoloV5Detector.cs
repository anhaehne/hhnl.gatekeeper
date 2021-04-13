using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using hhnl.gatekeeper.ImageProcessing.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;

namespace hhnl.gatekeeper.ImageProcessing.ObjectDetection
{
    public class YoloV5Detector
    {
        private static readonly object _modelLock = new();
        private static readonly MLContext _mlContext = new();
        private static readonly ConcurrentQueue<PredictionEngine<YoloV5BitmapData, YoloV5Prediction>> _predictionEngines = new();

        private static readonly ConcurrentQueue<TaskCompletionSource<PredictionEngine<YoloV5BitmapData, YoloV5Prediction>>>
            _waitingTasks = new();

        private static TransformerChain<OnnxTransformer>? _model;
        private static int _currentEnginesCount;
        private readonly ILogger<YoloV5Detector> _logger;

        private readonly IOptions<Options> _options;

        public YoloV5Detector(IOptions<Options> options, ILogger<YoloV5Detector> logger)
        {
            _options = options;
            _logger = logger;

            if (string.IsNullOrEmpty(options.Value.ModelPath))
                throw new ArgumentException("ModelPath can't be null or empty.", nameof(options));

            if (!File.Exists(options.Value.ModelPath))
                throw new ArgumentException("Model file doesn't exist.", nameof(options));

            if (options.Value.MaxEngineCount <= 0)
                throw new ArgumentException("MaxEngineCount must be greater than 0.", nameof(options));
        }

        public async Task<IReadOnlyList<YoloV5Result>> DetectAsync(IFrame frame)
        {
            _logger.LogTrace($"Detecting objects for frame {frame.Id}");

            var bitmap = await frame.ToScaledBitmapAsync(YoloV5Prediction.ModelWidth, YoloV5Prediction.ModelHeight);

            var engine = await GetPredictionEngineAsync();

            try
            {
                var prediction = engine.Predict(
                    new YoloV5BitmapData
                    {
                        Image = bitmap,
                        ImageWidth = frame.OriginalWidth,
                        ImageHeight = frame.OriginalHeight
                    });

                return prediction.GetResults(0.3f, 0.7f);
            }
            finally
            {
                ReturnPredicationEngine(engine);
            }
        }

        private Task<PredictionEngine<YoloV5BitmapData, YoloV5Prediction>> GetPredictionEngineAsync()
        {
            // Try get existing engine
            if (_predictionEngines.TryDequeue(out var engine))
                return Task.FromResult(engine);

            // If there are no engines left, check if we can create one.
            while (_currentEnginesCount < _options.Value.MaxEngineCount)
            {
                if (TryReserveEngineSpot())
                    return Task.FromResult(CreateEngine(_options.Value.ModelPath));
            }

            // If the maximum number of engines is reached, we wait for an engine to be returned.
            var tcs = new TaskCompletionSource<PredictionEngine<YoloV5BitmapData, YoloV5Prediction>>();
            _waitingTasks.Enqueue(tcs);
            return tcs.Task;

            bool TryReserveEngineSpot()
            {
                var current = _currentEnginesCount;

                if (current >= _options.Value.MaxEngineCount)
                    return false;

                var spot = current + 1;
                return Interlocked.CompareExchange(ref _currentEnginesCount, spot, current) == current;
            }
        }

        private static void ReturnPredicationEngine(PredictionEngine<YoloV5BitmapData, YoloV5Prediction> engine)
        {
            // Check if there are tasks waiting for an engine.
            if (_waitingTasks.TryDequeue(out var waitingTask))
            {
                waitingTask.SetResult(engine);
                return;
            }

            // Otherwise just return it to the queue.
            _predictionEngines.Enqueue(engine);
        }

        private PredictionEngine<YoloV5BitmapData, YoloV5Prediction> CreateEngine(string modelPath)
        {
            _logger.LogTrace("Creating predication engine.");
            return _mlContext.Model.CreatePredictionEngine<YoloV5BitmapData, YoloV5Prediction>(GetModel(modelPath));
        }

        private TransformerChain<OnnxTransformer> GetModel(string modelPath)
        {
            lock (_modelLock)
            {
                if (_model != null)
                    return _model;

                _logger.LogTrace("Creating predication model.");

                var pipeline = _mlContext.Transforms.ExtractPixels(inputColumnName: "bitmap",
                        outputColumnName: "images",
                        scaleImage: 1f / 255f,
                        interleavePixelColors: false)
                    .Append(_mlContext.Transforms.ApplyOnnxModel(
                        shapeDictionary: new Dictionary<string, int[]>
                        {
                            { "images", new[] { 1, 3, YoloV5Prediction.ModelHeight, YoloV5Prediction.ModelWidth } },
                            { "output", new[] { 1, 25200, 85 } }
                        },
                        inputColumnNames: new[]
                        {
                            "images"
                        },
                        outputColumnNames: new[]
                        {
                            "output"
                        },
                        modelFile: modelPath,
                        gpuDeviceId: 0));

                // Fit on empty list to obtain input data schema
                _model = pipeline.Fit(_mlContext.Data.LoadFromEnumerable(new List<YoloV5BitmapData>()));
                return _model;
            }
        }

        public class Options
        {
            public string ModelPath { get; set; }

            public int MaxEngineCount { get; set; } = 5;
        }
    }
}