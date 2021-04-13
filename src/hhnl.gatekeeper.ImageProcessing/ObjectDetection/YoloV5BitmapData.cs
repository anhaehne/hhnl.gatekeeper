using System.Drawing;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;

namespace hhnl.gatekeeper.ImageProcessing.ObjectDetection
{
    public class YoloV5BitmapData
    {
        [ColumnName("bitmap")]
        [ImageType(YoloV5Prediction.ModelHeight, YoloV5Prediction.ModelWidth)]
        public Bitmap Image { get; set; }

        [ColumnName("width")] 
        public float ImageWidth { get; set; }

        [ColumnName("height")] 
        public float ImageHeight { get; set; }
    }
}