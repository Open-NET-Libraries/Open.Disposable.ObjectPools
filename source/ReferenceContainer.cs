using System.Diagnostics;
using System.Threading;

namespace Open.Disposable
{
    [DebuggerDisplay("{Value,nq}")]
    public struct ReferenceContainer<T>
        where T : class
    {
        public T Value;

        public bool SetIfNull(T value)
        {
            if (Value == null)
            {
                Value = value;
                return true;
            }

            return false;
        }

        public bool TrySave(T value)
        {
            return Value == null
                && null == Interlocked.CompareExchange(ref Value, value, null);
        }

        public T TryRetrieve()
        {
            var item = Value;
            return (item != null
                && item == Interlocked.CompareExchange(ref Value, null, item))
                ? item
                : null;
        }
    }

}
