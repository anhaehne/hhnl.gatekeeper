using System.Collections.Generic;
using Google.Protobuf;
using hhnl.gatekeeper.ImageProcessing.Interfaces;
using hhnl.gatekeeper.ImageProcessing.ObjectDetection;
using MediatR;

namespace hhnl.gatekeeper.ImageProcessing.Messages
{
    public class NewObjectsMessage : INotification
    {
        public IFrame Frame { get; set; }

        public IReadOnlyCollection<ObjectResult> Results { get; set; }
    }
}