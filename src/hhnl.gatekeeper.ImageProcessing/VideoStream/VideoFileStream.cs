using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
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
        private readonly string _filePath;
        private MediaFile _mediaFile;

        static VideoFileStream()
        {
            FFmpegLoader.FFmpegPath = Path.GetFullPath("./../../../../../libs/ffmpeg");
        }
        
        public VideoFileStream(string filePath)
        {
            _filePath = filePath;
            _mediaFile = MediaFile.Open(_filePath);
        }

        // You have to install ffmpeg libraries. See: https://github.com/radek-k/FFMediaToolkit
        // Copy the dlls to libs/ffmpg.

        public Task<IFrame?> GetNextFrameAsync(CancellationToken cancellationToken = default)
        {
            if (!_mediaFile.Video.TryGetNextFrame(out var imageData))
                return Task.FromResult<IFrame?>(null);

            var bitmap = ToBitmap(imageData);
            
            return Task.FromResult<IFrame?>(new Frame(bitmap));
        }
        
        public unsafe Bitmap ToBitmap(ImageData bitmap)
        {
            fixed(byte* p = bitmap.Data)
            {
                return new Bitmap(bitmap.ImageSize.Width, bitmap.ImageSize.Height, bitmap.Stride, PixelFormat.Format24bppRgb, new IntPtr(p));
            }
        }
        
        private class Frame : IFrame
        {
            private readonly Bitmap _bitmap;

            public Frame(Bitmap bitmap)
            {
                _bitmap = bitmap;
            }
            
            public void Dispose()
            {
                _bitmap.Dispose();
            }

            public Task<Bitmap> ToScaledBitmapAsync(int width, int height)
            {
                var resized = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(resized);

                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.Low;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(_bitmap, 0, 0, width, height);

                return Task.FromResult(resized);
            }

            public int OriginalHeight => _bitmap.Height;

            public int OriginalWidth => _bitmap.Width;

            public long Id { get; }
        }
    }
}