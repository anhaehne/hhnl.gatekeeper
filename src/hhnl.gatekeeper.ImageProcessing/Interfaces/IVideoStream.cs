using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.gatekeeper.ImageProcessing.Interfaces
{
    public interface IVideoStream
    {
        Task<IFrame?> GetNextFrameAsync(CancellationToken cancellationToken = default);
    }
}