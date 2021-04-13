using hhnl.gatekeeper.ImageProcessing.Interfaces;
using MediatR;

namespace hhnl.gatekeeper.ImageProcessing.Messages
{
    public class NewFrameMessage : IRequest
    {
        public IFrame Frame { get; set; }
    }
}