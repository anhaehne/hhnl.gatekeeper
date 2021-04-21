using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.gatekeeper.ImageProcessing.Interfaces;
using hhnl.gatekeeper.ImageProcessing.Messages;
using hhnl.gatekeeper.ImageProcessing.ObjectDetection;
using MediatR;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Tracking;

namespace hhnl.gatekeeper.ImageProcessing.ObjectTracking
{
    public class ObjectTracker : INotificationHandler<NewObjectsMessage>, INotificationHandler<NewFrameMessage>
    {
        private readonly Context _context;

        public ObjectTracker(Context context)
        {
            _context = context;
        }

        public async Task Handle(NewFrameMessage request, CancellationToken cancellationToken)
        {
            if (!await _context.Semaphore.WaitAsync(TimeSpan.Zero, cancellationToken))
                return;

            try
            {
                using var lease = request.Frame.AddLease();

                if (!_context.TrackedObjects.Any() || _context.LastFrame == null)
                {
                    // Nothing to track yet
                    _context.LastFrame = request.Frame;
                    return;
                }

                var img = new Mat();
                Cv2.CvtColor(request.Frame.Original.ToMat(), img, ColorConversionCodes.RGBA2RGB);

                Parallel.ForEach(_context.TrackedObjects,
                    trackedObject =>
                    {
                        var newPosition = new Rect();

                        if (trackedObject.Tracker.Update(img, ref newPosition))
                            trackedObject.Position = newPosition;
                    });
            }
            finally
            {
                _context.Semaphore.Release();
            }
        }

        public async Task Handle(NewObjectsMessage request, CancellationToken cancellationToken)
        {
            await _context.Semaphore.WaitAsync(cancellationToken);


            try
            {
                if (!_context.TrackedObjects.Any())
                {
                    var img = new Mat();
                    Cv2.CvtColor(request.Frame.Original.ToMat(), img, ColorConversionCodes.RGBA2RGB);
                
                    // First detection; add all objects
                    _context.TrackedObjects.AddRange(request.Results.Select(r =>
                        new TrackedObject(r.Position, r.Class, img)));
                    return ;
                }
            }
            finally
            {
                _context.Semaphore.Release();
            }

            // if (_context.LastFrame is null)
            // {
            //     _context.LastFrame = request.Frame;
            //     _context.LastResults = request.Results;
            //     return Unit.Value;
            // }
            //
            // var m = new Mat<byte>(_context.LastFrame.OriginalHeight, _context.LastFrame.OriginalWidth);
            // m.SetTo(0);
            // var indexer = m.GetIndexer();
            //
            // var lastResult = _context.LastResults.Skip(15).First();
            //
            // for (var x = lastResult.Position.Left; x <= lastResult.Position.X2; x++)
            // {
            //     for (var y = lastResult.Position.Top; y <= lastResult.Position.Y2; y++)
            //     {
            //         indexer[y, x] = 255;
            //     }
            // }
            //
            // var lstGrey = new Mat();
            // var currentGrey = new Mat();
            //
            // Cv2.CvtColor(_context.LastFrame.Original.ToMat(), lstGrey, ColorConversionCodes.RGBA2GRAY);
            // Cv2.CvtColor(request.Frame.Original.ToMat(), currentGrey, ColorConversionCodes.RGBA2GRAY);
            //
            // var orb = ORB.Create(30);
            // var keyPoints = orb.Detect(lstGrey, m);
            // var features = keyPoints.Select(x => x.Pt).ToArray();
            //
            // var featuresOut = new Point2f[features.Length];
            //
            //
            // //
            // Cv2.CalcOpticalFlowPyrLK(lstGrey, currentGrey, features, ref featuresOut, out var status, out var err);
            //
            // var bestMatch = request.Results.Select(x => (Result: x, Count: features.Count(x.Position.Contains)))
            //     .Where(x => x.Count != 0).OrderBy(x => x.Count).FirstOrDefault();
            //
            //
            // {
            //     var resized = new Bitmap(request.Frame.OriginalWidth, request.Frame.OriginalHeight);
            //     using var graphics = Graphics.FromImage(resized);
            //
            //     graphics.CompositingQuality = CompositingQuality.HighSpeed;
            //     graphics.InterpolationMode = InterpolationMode.Low;
            //     graphics.CompositingMode = CompositingMode.SourceCopy;
            //     graphics.DrawImage(request.Frame.Original, 0, 0, request.Frame.OriginalWidth, request.Frame.OriginalHeight);
            //
            //     graphics.DrawRectangle(Pens.Green,
            //         lastResult.Position.Left,
            //         lastResult.Position.Top,
            //         lastResult.Position.Width,
            //         lastResult.Position.Height);
            //
            //     for (var i = 0; i < keyPoints.Length; i++)
            //     {
            //         graphics.DrawLine(Pens.Red, features[i].X, features[i].Y, featuresOut[i].X, featuresOut[i].Y);
            //     }
            //
            //     if (bestMatch != default)
            //     {
            //         graphics.DrawRectangle(Pens.Yellow,
            //             bestMatch.Result.Position.Left,
            //             bestMatch.Result.Position.Top,
            //             bestMatch.Result.Position.Width,
            //             bestMatch.Result.Position.Height);
            //     }
            //
            //     resized.Save(@$"C:\Users\andre\Desktop\Neuer Ordner\{request.Frame.Id}_track.png");
            // }
            //
            // {
            //     var resized = new Bitmap(_context.LastFrame.OriginalWidth, _context.LastFrame.OriginalHeight);
            //     using var graphics = Graphics.FromImage(resized);
            //
            //     graphics.CompositingQuality = CompositingQuality.HighSpeed;
            //     graphics.InterpolationMode = InterpolationMode.Low;
            //     graphics.CompositingMode = CompositingMode.SourceCopy;
            //
            //     graphics.DrawImage(_context.LastFrame.Original,
            //         0,
            //         0,
            //         _context.LastFrame.OriginalWidth,
            //         _context.LastFrame.OriginalHeight);
            //
            //     graphics.DrawRectangle(Pens.Green,
            //         lastResult.Position.Left,
            //         lastResult.Position.Top,
            //         lastResult.Position.Width,
            //         lastResult.Position.Height);
            //
            //     resized.Save(@$"C:\Users\andre\Desktop\Neuer Ordner\{request.Frame.Id}_prev.png");
            // }
            //
            // _context.LastFrame = request.Frame;
            // _context.LastResults = request.Results;
        }

        public class TrackedObject
        {
            private static int _idCounter;
            private Rect _position;

            public TrackedObject(Rect position, ObjectClass @class, Mat image)
            {
                _position = position;
                Class = @class;
                Id = Interlocked.Increment(ref _idCounter);
                Tracker = TrackerCSRT.Create();
                Tracker.Init(image, position);
            }

            public int Id { get; }

            public Rect Position
            {
                get => _position;
                set
                {
                    _position = value;
                    LastUpdated = DateTime.Now;
                }
            }

            public ObjectClass Class { get; }

            public DateTime LastUpdated { get; private set; }

            public Tracker Tracker { get; }
        }

        public class Context : IDisposable
        {
            private IFrame? _lastFrame;
            private IDisposable? _lastFrameLease;

            public IFrame? LastFrame
            {
                get => _lastFrame;
                set
                {
                    _lastFrameLease?.Dispose();

                    _lastFrame = value;
                    _lastFrameLease = value?.AddLease();
                }
            }

            public IReadOnlyCollection<ObjectResult> LastResults { get; set; }

            public List<TrackedObject> TrackedObjects { get; } = new();

            public SemaphoreSlim Semaphore { get; private set; } = new SemaphoreSlim(1);
            
            public void Dispose()
            {
                _lastFrameLease?.Dispose();
            }
        }
    }
}