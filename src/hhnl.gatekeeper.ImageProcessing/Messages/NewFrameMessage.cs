using hhnl.gatekeeper.ImageProcessing.Interfaces;
using MediatR;

namespace hhnl.gatekeeper.ImageProcessing.Messages
{
    public class NewFrameMessage : INotification
    {
        public IFrame Frame { get; set; }
    }
}