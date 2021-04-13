using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.gatekeeper.ImageProcessing.Messages;
using MediatR;
using Microsoft.Extensions.Options;

namespace hhnl.gatekeeper.ImageProcessing.ObjectDetection
{
    public class ObjectDetector : AsyncRequestHandler<NewFrameMessage>
    {
        private readonly YoloV5Detector _detector;
        private readonly IOptions<Options> _options;
        private readonly Context _context;

        public ObjectDetector(YoloV5Detector detector, IOptions<Options> options, Context context)
        {
            _detector = detector;
            _options = options;
            _context = context;
        }
        
        protected override async Task Handle(NewFrameMessage request, CancellationToken cancellationToken)
        {
            // Skip frames that are within the TimeBetweenDetection
            if (_context.LastDetectionTime + _options.Value.TimeBetweenDetection > DateTime.Now)
                return;
            
            _context.LastDetectionTime = DateTime.Now;
            
            var result = await _detector.DetectAsync(request.Frame);
        }

        public class Context
        {
            public DateTime LastDetectionTime { get; set; } = DateTime.MinValue;
        }
        
        public class Options
        {
            public TimeSpan TimeBetweenDetection { get; set; } = TimeSpan.FromSeconds(1);
        }
    }
} 