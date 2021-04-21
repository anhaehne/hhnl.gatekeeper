using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.gatekeeper.ImageProcessing.Interfaces;
using hhnl.gatekeeper.ImageProcessing.Messages;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace hhnl.gatekeeper.ImageProcessing.VideoStream
{
    public class VideoStreamProcessor : IDisposable
    {
        private readonly List<long> _latencies = new();
        private readonly ILogger<VideoStreamProcessor> _logger;
        private readonly IMediator _mediator;
        private readonly IOptions<Options> _options;
        private readonly ConcurrentDictionary<long, Task> _processingFrames = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private int _failedFrameCounter;
        private int _frameCount;
        private Timer? _metricsTimer;
        private Task? _processingTask;
        private IVideoStream? _videoStream;

        public VideoStreamProcessor(IMediator mediator, ILogger<VideoStreamProcessor> logger, IOptions<Options> options)
        {
            _mediator = mediator;
            _logger = logger;
            _options = options;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

        public void Start(IVideoStream videoStream)
        {
            _videoStream = videoStream;
            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = Task.Run(() => RunAsync(_cancellationTokenSource.Token));

            if (_options.Value.CollectMetrics)
                _metricsTimer = new Timer(DoMetrics, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void DoMetrics(object? state)
        {
            if (_frameCount == 0)
                return;

            var frameCount = Interlocked.Exchange(ref _frameCount, 0);
            _frameCount = 0;

            double avgLatencyMs;
            double maxLatencyMs;

            lock (_latencies)
            {
                avgLatencyMs = _latencies.Average();
                maxLatencyMs = _latencies.Max();
                _latencies.Clear();
            }

            _logger.LogInformation($"AvgLatency {avgLatencyMs:0.00} ms MaxLatency {maxLatencyMs:0.00} ms FPS: {frameCount}");
        }

        public void Stop()
        {
            // TODO: Do something with the frame processes in flight.

            _metricsTimer?.Dispose();

            _cancellationTokenSource?.Cancel();
            _processingTask?.GetAwaiter().GetResult();

            _videoStream = null;
            _processingTask = null;
            _cancellationTokenSource?.Dispose();
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                IFrame? frame;
                
                try
                {
                    frame = await _videoStream!.GetNextFrameAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    // If we reach the failed frame threshold we throw the exception, which will stop processing the stream.
                    if (_failedFrameCounter >= 30)
                        throw;

                    _logger.LogError(e, $"Failed to process frame get next frame.");
                    _failedFrameCounter++;
                    return;
                }
                
                if(frame is null)
                    break;
                
                StartProcessingFrame(frame, cancellationToken);
            }
        }

        private void StartProcessingFrame(IFrame frame, CancellationToken cancellationToken)
        {
            var task = ProcessFrameAsync(frame, cancellationToken);
            _processingFrames.TryAdd(frame.Id, task);

            task.ContinueWith(ProcessFrameAndRemoveTask, cancellationToken);
        }

        private void ProcessFrameAndRemoveTask(Task<IFrame> t)
        {
            _processingFrames.TryRemove(t.Result.Id, out _);
        }

        private async Task<IFrame> ProcessFrameAsync(IFrame frame, CancellationToken cancellationToken)
        {
            await Task.Yield();
            
            var start = Environment.TickCount64;

            var message = MessagePool<NewFrameMessage>.Get();
            message.Frame = frame;
            using var lease = frame.AddLease();

            try
            {
                // Process frame
                await _mediator.Publish(message, cancellationToken);
            }
            catch (Exception e)
            {
                // If we reach the failed frame threshold we throw the exception, which will stop processing the stream.
                if (_failedFrameCounter >= 30)
                    throw;

                _logger.LogError(e, $"Failed to process frame {frame.Id}");
                _failedFrameCounter++;
            }
            finally
            {
                MessagePool<NewFrameMessage>.Return(message);
                lease.Dispose();
            }

            // Reset failed frame counter
            _failedFrameCounter = 0;

            // Do metrics
            if (_options.Value.CollectMetrics)
            {
                lock (_latencies)
                {
                    _latencies.Add(Environment.TickCount64 - start);
                }

                Interlocked.Increment(ref _frameCount);
            }

            return frame;
        }

        public class Options
        {
            public bool CollectMetrics { get; set; }
        }
    }
}