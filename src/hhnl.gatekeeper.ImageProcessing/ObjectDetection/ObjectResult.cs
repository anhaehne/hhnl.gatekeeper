using hhnl.gatekeeper.ImageProcessing.Models;
using OpenCvSharp;

namespace hhnl.gatekeeper.ImageProcessing.ObjectDetection
{
    public class ObjectResult
    {
        public ObjectResult(YoloV5Result yoloV5Result)
        {
            var width = yoloV5Result.BBox[2] - yoloV5Result.BBox[0];
            var height = yoloV5Result.BBox[3] - yoloV5Result.BBox[1];
            
            Position = new Rect((int)yoloV5Result.BBox[0], (int)yoloV5Result.BBox[1], (int)width, (int)height);
            Class = (ObjectClass)yoloV5Result.Class;
        }

        public Rect Position { get; }

        public ObjectClass Class { get; }

    }
}