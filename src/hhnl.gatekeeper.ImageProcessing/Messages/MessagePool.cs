using Microsoft.Extensions.ObjectPool;

namespace hhnl.gatekeeper.ImageProcessing.Messages
{
    public static class MessagePool<T> where T : class, new()
    {
        private static readonly ObjectPool<T> _pool = new DefaultObjectPool<T>(new DefaultPooledObjectPolicy<T>());

        public static T Get()
        {
            return _pool.Get();
        }

        public static void Return(T message)
        {
            _pool.Return(message);
        }
    }
}