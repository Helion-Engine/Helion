using System;
using System.Runtime.CompilerServices;

namespace NFluidsynth
{
    /// <summary>
    ///     Provides basic functionality for all native Fluidsynth objects.
    /// </summary>
    public abstract class FluidsynthObject : IDisposable
    {
        protected FluidsynthObject(IntPtr handle)
        {
            Handle = handle;
        }

        public bool Disposed { get; private set; }

        /// <summary>
        ///     Handle to the native Fluidsynth C object.
        /// </summary>
        public IntPtr Handle { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Disposed = true;
        }

        public override int GetHashCode()
        {
            return (int) Handle;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FluidsynthObject);
        }

        protected virtual bool Equals(FluidsynthObject obj)
        {
            return obj != null && Handle == obj.Handle;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(FluidsynthObject));
            }
        }

        ~FluidsynthObject()
        {
            Dispose(false);
        }
    }
}