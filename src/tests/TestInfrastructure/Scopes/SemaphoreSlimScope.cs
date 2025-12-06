namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using System.Collections.Generic;
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

        public static async ValueTask<SemaphoreSlimLeaseCollection> WaitAsync(
            SemaphoreSlim semaphore,
            int leaseCount,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(semaphore);

            if (leaseCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(leaseCount),
                    leaseCount,
                    "Lease count must be positive."
                );
            }

            SemaphoreSlimLeaseCollection collection = new SemaphoreSlimLeaseCollection();
            try
            {
                for (int index = 0; index < leaseCount; index++)
                {
                    SemaphoreSlimLease lease = await WaitAsync(semaphore, cancellationToken)
                        .ConfigureAwait(false);
                    collection.Add(lease);
                }

                return collection;
            }
            catch
            {
                await collection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
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

    /// <summary>
    /// Represents a disposable collection of <see cref="SemaphoreSlimLease"/> instances.
    /// </summary>
    internal sealed class SemaphoreSlimLeaseCollection : IAsyncDisposable, IDisposable
    {
        private readonly List<SemaphoreSlimLease> _leases = new List<SemaphoreSlimLease>();
        private bool _disposed;

        public void Add(SemaphoreSlimLease lease)
        {
            ArgumentNullException.ThrowIfNull(lease);

            _leases.Add(lease);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            for (int index = 0; index < _leases.Count; index++)
            {
                _leases[index].Dispose();
            }

            _leases.Clear();
            _disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
