using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ML.Data;

namespace hhnl.gatekeeper.ImageProcessing.ObjectDetection
{
    public class YoloV5Prediction
    {
        public const int ModelWidth = 640;
        public const int ModelHeight = 640;
        private static readonly int _classLength = Enum.GetValues(typeof(YoloV5ObjectClass)).Length;

        /// <summary>
        /// Identity
        /// </summary>
        [VectorType(1, 25200, 85)]
        [ColumnName("output")]
        public float[] Output { get; set; }

        [ColumnName("width")] public float ImageWidth { get; set; }

        [ColumnName("height")] public float ImageHeight { get; set; }

        public IReadOnlyList<YoloV5Result> GetResults(float scoreThres = 0.5f, float iouThres = 0.5f)
        {
            // Probabilities + Characteristics
            var characteristics = _classLength + 5;

            // Needed info
            var xGain = ModelWidth / ImageWidth;
            var yGain = ModelHeight / ImageHeight;
            var results = Output.AsSpan();

            List<float[]?> postProcessedResults = new();

            // For every cell of the image, format for NMS
            for (var i = 0; i < 25200; i++)
            {
                // Get offset in float array
                var offset = characteristics * i;

                // Get a prediction cell
                var predCell = results.Slice(offset, characteristics);

                // Filter some boxes
                var objConf = predCell[4];
                if (objConf <= scoreThres) continue;

                // Get corners in original shape
                var x1 = (predCell[0] - predCell[2] / 2) / xGain; //top left x
                var y1 = (predCell[1] - predCell[3] / 2) / yGain; //top left y
                var x2 = (predCell[0] + predCell[2] / 2) / xGain; //bottom right x
                var y2 = (predCell[1] + predCell[3] / 2) / yGain; //bottom right y

                var classProbabilities = predCell.Slice(5, _classLength);

                // Get best class and index
                var (maxConf, maxClass) = GetMaxClassConf(classProbabilities, objConf);

                postProcessedResults.Add(new[] { x1, y1, x2, y2, maxConf, maxClass });
            }

            var resultsNms = ApplyNMS(postProcessedResults, iouThres);

            return resultsNms;
        }

        private static (float MaxConf, float Class) GetMaxClassConf(Span<float> values, float objConf)
        {
            var max = float.MinValue;
            var maxIndex = -1;

            for (var i = 0; i < values.Length; i++)
            {
                var current = values[i] * objConf;

                if (!(current > max))
                    continue;

                max = current;
                maxIndex = i;
            }

            return (max, maxIndex);
        }

        private List<YoloV5Result> ApplyNMS(List<float[]?> postProcessedResults, float iouThres = 0.5f)
        {
            postProcessedResults = postProcessedResults.OrderByDescending(x => x[4]).ToList(); // sort by confidence
            List<YoloV5Result> resultsNms = new();

            var f = 0;

            while (f < postProcessedResults.Count)
            {
                var res = postProcessedResults[f];

                if (res == null)
                {
                    f++;
                    continue;
                }

                var conf = res[4];

                resultsNms.Add(new YoloV5Result(res.Take(4).ToArray(), (YoloV5ObjectClass)(int)res[5], conf));
                postProcessedResults[f] = null;

                var iou = postProcessedResults.Select(bbox => bbox == null ? float.NaN : BoxIoU(res, bbox)).ToList();

                for (var i = 0; i < iou.Count; i++)
                {
                    if (float.IsNaN(iou[i])) continue;

                    if (iou[i] > iouThres)
                        postProcessedResults[i] = null;
                }

                f++;
            }

            return resultsNms;
        }

        /// <summary>
        /// Return intersection-over-union (Jaccard index) of boxes.
        /// <para>Both sets of boxes are expected to be in (x1, y1, x2, y2) format.</para>
        /// </summary>
        private static float BoxIoU(float[] boxes1, float[] boxes2)
        {
            static float box_area(float[] box)
            {
                return (box[2] - box[0]) * (box[3] - box[1]);
            }

            var area1 = box_area(boxes1);
            var area2 = box_area(boxes2);

            Debug.Assert(area1 >= 0);
            Debug.Assert(area2 >= 0);

            var dx = Math.Max(0, Math.Min(boxes1[2], boxes2[2]) - Math.Max(boxes1[0], boxes2[0]));
            var dy = Math.Max(0, Math.Min(boxes1[3], boxes2[3]) - Math.Max(boxes1[1], boxes2[1]));
            var inter = dx * dy;

            return inter / (area1 + area2 - inter);
        }
    }
}