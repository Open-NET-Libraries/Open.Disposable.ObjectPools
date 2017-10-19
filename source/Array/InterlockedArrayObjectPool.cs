/* Based on Roslyn's ObjectPool */

using System;
using System.Threading;

namespace Open.Disposable
{
    /// <summary>
    /// An extremely fast ObjectPool when the capacity is in the low 100s.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InterlockedArrayObjectPool<T> : ObjectPoolBase<T>
        where T : class
    {

        public InterlockedArrayObjectPool(Func<T> factory, Action<T> recycler, int capacity = DEFAULT_CAPACITY)
            : base(factory, recycler, capacity)
        {
            Pool = new ReferenceContainer<T>[capacity - 1];
        }

        public InterlockedArrayObjectPool(Func<T> factory, int capacity = DEFAULT_CAPACITY)
            : this(factory, null, capacity)
        { }

        protected ReferenceContainer<T>[] Pool;

        // Sets a limit on what has been stored yet to prevent over searching the array unnecessarily.. 
        protected int MaxStored = 0;
        protected const int MaxStoredIncrement = 5; // Instead of every one.

        protected override bool Receive(T item)
        {
            var elements = Pool;
            var len = elements?.Length ?? 0;

            for (int i = 0; i < len; i++)
            {
                if (elements[i].TrySave(item))
                {
                    var m = MaxStored;
                    if (i >= m) Interlocked.CompareExchange(ref MaxStored, m + MaxStoredIncrement, m);

                    return true;
                }
            }

            return false;
        }

        protected override T TryRelease()
        {
            // We missed getting the first item or it wasn't there.
            var elements = Pool;
            if (elements == null) return null;

            var len = elements.Length;
            for (int i = 0; i < MaxStored && i < len; i++)
            {
                var item = elements[i].TryRetrieve();
                if (item != null) return item;
            }

            return null;
        }

        protected override void OnDispose(bool calledExplicitly)
        {
            Pool = null;
            MaxStored = 0;
        }

    }

    public static class InterlockedArrayObjectPool
    {
        public static InterlockedArrayObjectPool<T> Create<T>(Func<T> factory, int capacity = Constants.DEFAULT_CAPACITY)
            where T : class
        {
            return new InterlockedArrayObjectPool<T>(factory, capacity);
        }

        public static InterlockedArrayObjectPool<T> Create<T>(int capacity = Constants.DEFAULT_CAPACITY)
            where T : class, new()
        {
            return Create(() => new T(), capacity);
        }
    }
}
