namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;

    /// <summary>
    /// A disposable struct that wraps a pooled resource and automatically returns it to the pool when disposed.
    /// </summary>
    /// <typeparam name="T">The type of the pooled resource.</typeparam>
    /// <remarks>
    /// <para>
    /// This struct is designed for use with 'using' statements to ensure pooled resources are
    /// properly returned even when exceptions occur. The pattern is:
    /// </para>
    /// <code>
    /// using (PooledResource&lt;DynValue[]&gt; pooled = DynValueArrayPool.Rent(8, out DynValue[] array))
    /// {
    ///     // Use array...
    /// } // Automatically returned to pool here
    /// </code>
    /// <para>
    /// This follows the same pattern used by WallstopStudios.UnityHelpers for zero-allocation pooling.
    /// </para>
    /// </remarks>
    internal struct PooledResource<T> : IDisposable
    {
        /// <summary>
        /// The pooled resource instance.
        /// </summary>
        public T Resource { get; }

        private readonly Action<T> _onDispose;
        private bool _disposed;

        /// <summary>
        /// Creates a new <see cref="PooledResource{T}"/> wrapping the specified resource.
        /// </summary>
        /// <param name="resource">The resource to wrap.</param>
        /// <param name="onDispose">The action to invoke when disposing (typically returns the resource to the pool).</param>
        public PooledResource(T resource, Action<T> onDispose)
        {
            Resource = resource;
            _onDispose = onDispose;
            _disposed = false;
        }

        /// <summary>
        /// Disposes the resource by invoking the disposal action, typically returning it to the pool.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _onDispose?.Invoke(Resource);
        }
    }
}
