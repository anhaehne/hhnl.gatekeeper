using System.Buffers;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FFMediaToolkit;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using hhnl.gatekeeper.ImageProcessing.Interfaces;

namespace hhnl.gatekeeper.ImageProcessing.VideoStream
{
    public class VideoFileStream : IVideoStream
    {
        private readonly MediaFile _mediaFile;

        static VideoFileStream()
        {
            FFmpegLoader.FFmpegPath = Path.GetFullPath("./../../../../../libs/ffmpeg");
        }

        public VideoFileStream(string filePath)
        {
            _mediaFile = MediaFile.Open(filePath);
        }

        // You have to install ffmpeg libraries. See: https://github.com/radek-k/FFMediaToolkit
        // Copy the dlls to libs/ffmpg.
        public Task<IFrame?> GetNextFrameAsync(CancellationToken cancellationToken = default)
        {
            Thread.Sleep(30);

            if (!_mediaFile.Video.TryGetNextFrame(out var imageData))
                return Task.FromResult<IFrame?>(null);

            return Task.FromResult<IFrame?>(new Frame(imageData));
        }


        private class Frame : ManagedObjectBase, IFrame
        {
            private static long _frameCounter;
            private readonly byte[] _pixels;

            public Frame(ImageData image)
            {
                (Original, _pixels) = ToBitmap(image);
                Id = Interlocked.Increment(ref _frameCounter);
            }

            protected override void Dispose()
            {
                ArrayPool<byte>.Shared.Return(_pixels);
            }

            public Bitmap Original { get; }

            public Task<Bitmap> ToScaledBitmapAsync(int width, int height)
            {
                var resized = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(resized);

                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.Low;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(Original, 0, 0, width, height);

                return Task.FromResult(resized);
            }

            public int OriginalHeight => Original.Height;

            public int OriginalWidth => Original.Width;

            public long Id { get; }


            private (Bitmap bitmap, byte[] arr) ToBitmap(ImageData image)
            {
                Bitmap bmp = new(image.ImageSize.Width, image.ImageSize.Height);

                BitmapData data = bmp.LockBits(new Rectangle(0, 0, image.ImageSize.Width, image.ImageSize.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format24bppRgb);

                var array = ArrayPool<byte>.Shared.Rent(image.Data.Length);
                image.Data.CopyTo(array);

                Marshal.Copy(array, 0, data.Scan0, data.Stride * data.Height);

                bmp.UnlockBits(data);
                return (bmp, array);
            }
        }
    }
}