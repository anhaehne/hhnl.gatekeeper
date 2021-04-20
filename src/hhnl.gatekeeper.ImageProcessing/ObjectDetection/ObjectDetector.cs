using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.gatekeeper.ImageProcessing.Messages;
using MediatR;
using Microsoft.Extensions.Options;

namespace hhnl.gatekeeper.ImageProcessing.ObjectDetection
{
    public class ObjectDetector : AsyncRequestHandler<NewFrameMessage>
    {
        private readonly Context _context;
        private readonly YoloV5Detector _detector;
        private readonly IMediator _mediator;
        private readonly IOptions<Options> _options;
        private readonly List<YoloV5ObjectClass> _relevantClasses;

        public ObjectDetector(YoloV5Detector detector, IOptions<Options> options, Context context, IMediator mediator)
        {
            _detector = detector;
            _options = options;
            _context = context;
            _mediator = mediator;

            _relevantClasses = _options.Value.Classes.Select(Enum.Parse<YoloV5ObjectClass>).ToList();
        }

        protected override async Task Handle(NewFrameMessage request, CancellationToken cancellationToken)
        {
            // Skip frames that are within the TimeBetweenDetection
            if (_context.LastDetectionTime + _options.Value.TimeBetweenDetection > DateTime.Now)
                return;

            using var lease = request.Frame.AddLease();
            
            _context.LastDetectionTime = DateTime.Now;

            var result = await _detector.DetectAsync(request.Frame, _relevantClasses);

            await _mediator.Send(new NewObjectsMessage
                {
                    Frame = request.Frame,
                    Results = result.Select(x => new ObjectResult(x)).ToList()
                },
                cancellationToken);

            // if (result.Any())
            // {
            //     var resized = new Bitmap(request.Frame.OriginalWidth, request.Frame.OriginalHeight);
            //     using var graphics = Graphics.FromImage(resized);
            //
            //     graphics.CompositingQuality = CompositingQuality.HighSpeed;
            //     graphics.InterpolationMode = InterpolationMode.Low;
            //     graphics.CompositingMode = CompositingMode.SourceCopy;
            //     graphics.DrawImage(request.Frame.Original, 0, 0, request.Frame.OriginalWidth, request.Frame.OriginalHeight);
            //
            //
            //     foreach (var r in result)
            //     {
            //         graphics.DrawRectangle(Pens.Red, r.BBox[0], r.BBox[1], r.BBox[2] - r.BBox[0], r.BBox[3] - r.BBox[1]);
            //     }
            //
            //     resized.Save(@$"C:\Users\andre\Desktop\Neuer Ordner\{request.Frame.Id}.png");
            // }
        }

        public class Context
        {
            public DateTime LastDetectionTime { get; set; } = DateTime.MinValue;
        }

        public class Options
        {
            public TimeSpan TimeBetweenDetection { get; set; } = TimeSpan.FromSeconds(1);

            public string[] Classes { get; set; } = Array.Empty<string>();
        }
    }
}