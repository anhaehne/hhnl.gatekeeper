using System;
using System.Buffers;
using System.Collections.Generic;
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
            using var first = GetEncodings(image1).First();
            using var sec = GetEncodings(image2).First();

            return FaceRecognition.FaceDistance(first, sec);
        }

        public void Test(string s)
        {
            using (var unknownImage = FaceRecognition.LoadImageFile(s))
            {
                var faceLocations = fr.FaceLocations(unknownImage, 3).ToArray();

                Console.WriteLine(faceLocations.Length);
                
                foreach (var faceLocation in faceLocations)
                    PrintResult(s, faceLocation);
            }
        }
        
        private static void PrintResult(string filename, Location location)
        {
            Console.WriteLine($"{filename},{location.Top},{location.Right},{location.Bottom},{location.Left}");
        }
        
        public void EncodeImage(Stream image)
        {
            var encodings = GetEncodings(image);

            foreach (var encoding in encodings)
            {
                
                encoding.Dispose();
            }
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

        private IEnumerable<FaceEncoding> GetEncodings(Stream image)
        {
            using var sourceBitmap = SKBitmap.Decode(image);

            using var defaultImageArray = Dlib.LoadImageData<RgbPixel>(ImagePixelFormat.Bgra,
                sourceBitmap.Bytes,
                (uint)sourceBitmap.Height,
                (uint)sourceBitmap.Width,
                (uint)sourceBitmap.RowBytes);
            var dmatrix =  new Matrix<RgbPixel>(defaultImageArray);
            var dfaceImage = (Image)_imageConstructor.Invoke(new object[] { dmatrix, Mode.Rgb });
            var dLocations = fr.FaceLocations(dfaceImage).ToList();
            
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

            var faceLocations = fr.FaceLocations(faceImage);
            var encodings = fr.FaceEncodings(faceImage, faceLocations);

            return encodings;
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