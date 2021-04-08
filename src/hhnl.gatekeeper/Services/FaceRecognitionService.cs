using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DlibDotNet;
using FaceRecognitionDotNet;
using SkiaSharp;

namespace hhnl.gatekeeper.Services
{
    public class FaceRecognitionService
    {
        private const int MAX_SIZE = 200;
        static FaceRecognition fr = FaceRecognition.Create(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

        public double RecognizeFacesInImage(Stream image1, Stream image2)
        {
            using var first = GetEncoding(image1);
            using var sec = GetEncoding(image2);

            return FaceRecognition.FaceDistance(first, sec);
        }


        private (int Width, int Height) GetResizedImageSize(SKBitmap sourceBitmap)
        {
            if (sourceBitmap.Height >= sourceBitmap.Width)
            {
                // Portrait
                var ration = sourceBitmap.Height / (double)sourceBitmap.Width;
                return ((int)(MAX_SIZE / ration), MAX_SIZE);
            }
            else
            {
                // Landscape
                var ration = sourceBitmap.Width / (double)sourceBitmap.Height;
                return (MAX_SIZE, (int)(MAX_SIZE / ration));
            }
        }

        private FaceEncoding GetEncoding(Stream image)
        {
            using var sourceBitmap = SKBitmap.Decode(image);
            var (newWidth, newHeight) = GetResizedImageSize(sourceBitmap);
            using var scaledBitmap = Scale(sourceBitmap, newWidth , newHeight);

            using var array = Dlib.LoadImageData<RgbPixel>(
                ImagePixelFormat.Bgra,
                scaledBitmap.Bytes,
                (uint)scaledBitmap.Height,
                (uint)scaledBitmap.Width,
                (uint)scaledBitmap.RowBytes);

            var matrix =  new Matrix<RgbPixel>(array);

            var faceImage = (Image)_imageConstructor.Invoke(new object[] { matrix, Mode.Rgb });

            var t = fr.FaceEncodings(faceImage);

            return t.Single();
        }

        private SKBitmap Scale(SKBitmap input, int width, int height)
        {
            var info = new SKImageInfo(width, height);
            var newImg = SKImage.Create(info);
            input.ScalePixels(newImg.PeekPixels(), SKFilterQuality.Medium);
            return SKBitmap.FromImage(newImg);
        }

        private static readonly ConstructorInfo _imageConstructor =
            typeof(Image).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(MatrixBase), typeof(Mode) }, null);


    }
}