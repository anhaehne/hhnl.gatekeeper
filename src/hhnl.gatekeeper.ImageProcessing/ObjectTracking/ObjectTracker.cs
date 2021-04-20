using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.gatekeeper.ImageProcessing.Interfaces;
using hhnl.gatekeeper.ImageProcessing.Messages;
using MediatR;
using OpenCvSharp.OptFlow;

namespace hhnl.gatekeeper.ImageProcessing.ObjectTracking
{
    public class ObjectTracker : AsyncRequestHandler<NewObjectsMessage>
    {
        private readonly Context _context;

        public ObjectTracker(Context context)
        {
            _context = context;
        }
        
        protected override async Task Handle(NewObjectsMessage request, CancellationToken cancellationToken)
        {
            if (_context.LastFrame is null)
            {
                _context.LastFrame = request.Frame;
                return;
            }

            // Do tracking

            _context.LastFrame = request.Frame;
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

            public void Dispose()
            {
                _lastFrameLease?.Dispose();
            }
        }
    }
}