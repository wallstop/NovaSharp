namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// A thread-safe generic object pool that manages reusable instances of type T.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    /// <remarks>
    /// This implementation follows the WallstopStudios.UnityHelpers pattern:
    /// - Thread-safe via ConcurrentStack
    /// - RAII pattern via PooledResource
    /// - Configurable callbacks for get/release/dispose lifecycle
    /// </remarks>
    internal sealed class GenericPool<T> : IDisposable
    {
        private readonly Func<T> _producer;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDispose;
        private readonly ConcurrentStack<T> _pool = new();
        private readonly int _maxPoolSize;
        private bool _disposed;

        /// <summary>
        /// Gets the current number of instances in the pool.
        /// </summary>
        internal int Count => _pool.Count;

        /// <summary>
        /// Creates a new generic pool with the specified producer function and optional callbacks.
        /// </summary>
        /// <param name="producer">Function that creates new instances when the pool is empty.</param>
        /// <param name="preWarmCount">Number of instances to create during initialization.</param>
        /// <param name="maxPoolSize">Maximum number of instances to keep pooled. Default 64.</param>
        /// <param name="onGet">Optional callback invoked when an instance is retrieved.</param>
        /// <param name="onRelease">Optional callback invoked when an instance is returned.</param>
        /// <param name="onDispose">Optional callback invoked for each instance when pool is disposed.</param>
        public GenericPool(
            Func<T> producer,
            int preWarmCount = 0,
            int maxPoolSize = 64,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDispose = null
        )
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _maxPoolSize = maxPoolSize > 0 ? maxPoolSize : 64;
            _onGet = onGet;
            _onRelease = onRelease;
            _onDispose = onDispose;

            for (int i = 0; i < preWarmCount && i < _maxPoolSize; i++)
            {
                T value = _producer();
                _onGet?.Invoke(value);
                Return(value);
            }
        }

        /// <summary>
        /// Gets a pooled resource. When disposed, the resource is automatically returned to the pool.
        /// </summary>
        /// <returns>A PooledResource wrapping the retrieved instance.</returns>
        public PooledResource<T> Get()
        {
            return Get(out _);
        }

        /// <summary>
        /// Gets a pooled resource and outputs the value.
        /// </summary>
        /// <param name="value">The retrieved instance.</param>
        /// <returns>A PooledResource wrapping the retrieved instance.</returns>
        public PooledResource<T> Get(out T value)
        {
            if (!_pool.TryPop(out value))
            {
                value = _producer();
            }

            _onGet?.Invoke(value);
            return new PooledResource<T>(value, Return);
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        /// <param name="value">The instance to return.</param>
        public void Return(T value)
        {
            if (_disposed)
            {
                _onDispose?.Invoke(value);
                return;
            }

            _onRelease?.Invoke(value);

            if (_pool.Count < _maxPoolSize)
            {
                _pool.Push(value);
            }
            else
            {
                _onDispose?.Invoke(value);
            }
        }

        /// <summary>
        /// Disposes the pool, invoking onDispose for each pooled instance if provided.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_onDispose == null)
            {
                _pool.Clear();
                return;
            }

            while (_pool.TryPop(out T value))
            {
                _onDispose(value);
            }
        }
    }
}
