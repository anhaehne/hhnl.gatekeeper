namespace hhnl.gatekeeper.ImageProcessing.ObjectDetection
{
    public class ObjectResult
    {
        public ObjectResult(YoloV5Result yoloV5Result)
        {
            Left = (int)yoloV5Result.BBox[0];
            Top = (int)yoloV5Result.BBox[1];
            Width = (int)(yoloV5Result.BBox[0] - yoloV5Result.BBox[2]);
            Height = (int)(yoloV5Result.BBox[1] - yoloV5Result.BBox[3]);
            Class = (ObjectClass)yoloV5Result.Class;
        }

        public int Top { get; }

        public int Left { get; }

        public int Width { get; }

        public int Height { get; }

        public ObjectClass Class { get; }
    }
}