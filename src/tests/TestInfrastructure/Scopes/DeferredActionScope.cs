namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;

    /// <summary>
    /// Executes a supplied callback when disposed unless suppressed.
    /// </summary>
    internal sealed class DeferredActionScope : IDisposable
    {
        private Action _onDispose;
        private bool _disposed;

        private DeferredActionScope(Action onDispose)
        {
            ArgumentNullException.ThrowIfNull(onDispose);
            _onDispose = onDispose;
        }

        public static DeferredActionScope Run(Action onDispose)
        {
            return new DeferredActionScope(onDispose);
        }

        public void Suppress()
        {
            _onDispose = null;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _onDispose?.Invoke();
            _onDispose = null;
        }
    }
}
