namespace hhnl.gatekeeper.ImageProcessing.ObjectDetection
{
    public class YoloV5Result
    {
        /// <summary>
        /// x1, y1, x2, y2 in page coordinates.
        /// <para>left, top, right, bottom.</para>
        /// </summary>
        public float[] BBox { get; }

        /// <summary>
        /// The Bbox category.
        /// </summary>
        public YoloV5ObjectClass Class { get; }

        /// <summary>
        /// Confidence level.
        /// </summary>
        public float Confidence { get; }

        public YoloV5Result(float[] bbox, YoloV5ObjectClass @class, float confidence)
        {
            BBox = bbox;
            Class = @class;
            Confidence = confidence;
        }
    }
}
