using System;
using System.Threading;
using hhnl.gatekeeper.ImageProcessing.Interfaces;

namespace hhnl.gatekeeper.ImageProcessing.Models
{
    public abstract class ManagedObjectBase : IManagedObject
    {
        private int _leaseCount = 0;
        
        public IDisposable AddLease()
        {
            Interlocked.Increment(ref _leaseCount);
            return new Lease(this);
        }

        private void RemoveLease()
        {
            if(Interlocked.Decrement(ref _leaseCount) == 0)
                Dispose();
        }

        protected abstract void Dispose();

        private class Lease : IDisposable
        {
            private readonly ManagedObjectBase _managedObject;
            private bool _isDisposed = false;

            public Lease(ManagedObjectBase managedObject)
            {
                _managedObject = managedObject;
            }

            public void Dispose()
            {
                if (_isDisposed)
                    return;
                
                _managedObject.RemoveLease();
                _isDisposed = true;
            }
        }
    }
}