using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using hhnl.gatekeeper.ImageProcessing.Interfaces;
using Microsoft.Extensions.ObjectPool;

namespace hhnl.gatekeeper.ImageProcessing.VideoStream
{
    /// <summary>
    /// Helper class to emulate a video stream from a single image.
    /// </summary>
    public class ImageFileStream : IVideoStream
    {
        private readonly ObjectPool<Bitmap> _bitmapPool;
        private readonly int _fps;

        /// <summary>
        /// The path of image file to emulate the video
        /// </summary>
        /// <param name="filePath">The path of image file to emulate the video stream with.</param>
        /// <param name="fps">
        /// The target fps. This will not be exact since timers wont be accurate enough.
        /// Specify -1 to only emit one frame and then wait indefinitely.
        /// </param>
        public ImageFileStream(string filePath, int fps = -1)
        {
            _fps = fps;
            _bitmapPool = new DefaultObjectPool<Bitmap>(new PooledBitmapPolicy(filePath));
        }

        public async IAsyncEnumerable<IFrame> Frames([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_fps == -1)
            {
                yield return new Frame(_bitmapPool);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            var frameDelay = TimeSpan.FromSeconds(1) / _fps;

            while (!cancellationToken.IsCancellationRequested)
            {
                yield return new Frame(_bitmapPool);
                await Task.Delay(frameDelay, cancellationToken);
            }
        }

        private class PooledBitmapPolicy : IPooledObjectPolicy<Bitmap>
        {
            private readonly string _filePath;

            public PooledBitmapPolicy(string filePath)
            {
                _filePath = filePath;
            }


            public Bitmap Create()
            {
                return new(Image.FromFile(_filePath));
            }

            public bool Return(Bitmap obj)
            {
                return true;
            }
        }

        private class Frame : ManagedObjectBase, IFrame
        {
            private static long _frameCounter;
            private readonly Bitmap _original;
            private readonly ObjectPool<Bitmap> _originalPool;

            public Frame(ObjectPool<Bitmap> originalPool)
            {
                _originalPool = originalPool;
                _original = originalPool.Get();
                Id = Interlocked.Increment(ref _frameCounter);
            }

            protected override void Dispose()
            {
                _originalPool.Return(_original);
            }

            public Bitmap Original => _original;

            public Task<Bitmap> ToScaledBitmapAsync(int width, int height)
            {
                var resized = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(resized);

                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.Low;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(_original, 0, 0, width, height);

                return Task.FromResult(resized);
            }

            public int OriginalHeight => _original.Height;

            public int OriginalWidth => _original.Width;

            public long Id { get; }
        }

        public Task<IFrame?> GetNextFrameAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}