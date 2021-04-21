using System;

namespace hhnl.gatekeeper.ImageProcessing.Interfaces
{
    public interface IManagedObject
    {
        IDisposable AddLease();
    }
}