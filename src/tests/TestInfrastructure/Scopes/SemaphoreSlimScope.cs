namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides awaitable leases for <see cref="SemaphoreSlim"/> instances.
    /// </summary>
    internal static class SemaphoreSlimScope
    {
        public static async ValueTask<SemaphoreSlimLease> WaitAsync(
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(semaphore);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new SemaphoreSlimLease(semaphore);
        }
    }

    /// <summary>
    /// Represents a disposable lease over a <see cref="SemaphoreSlim"/>.
    /// </summary>
    internal sealed class SemaphoreSlimLease : IAsyncDisposable, IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public SemaphoreSlimLease(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _semaphore.Release();
            _disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
