namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NovaSharp.Interpreter.Platforms;

    /// <summary>
    /// Serializes tests that mutate <see cref="PlatformAutoDetector"/> state and restores the snapshot on disposal.
    /// </summary>
    internal sealed class PlatformDetectorIsolationScope : IAsyncDisposable, IDisposable
    {
        private static readonly SemaphoreSlim IsolationGate = new(1, 1);
        private readonly PlatformAutoDetector.PlatformDetectorSnapshot _snapshot;
        private bool _gateHeld;
        private bool _disposed;

        private PlatformDetectorIsolationScope(
            PlatformAutoDetector.PlatformDetectorSnapshot snapshot,
            bool gateHeld
        )
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _gateHeld = gateHeld;
        }

        public static async Task<PlatformDetectorIsolationScope> EnterAsync()
        {
            await IsolationGate.WaitAsync().ConfigureAwait(false);
            PlatformAutoDetector.PlatformDetectorSnapshot snapshot =
                PlatformAutoDetector.TestHooks.CaptureState();
            return new PlatformDetectorIsolationScope(snapshot, gateHeld: true);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            PlatformAutoDetector.TestHooks.RestoreState(_snapshot);
            ReleaseGate();

            _disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private void ReleaseGate()
        {
            if (!_gateHeld)
            {
                return;
            }

            IsolationGate.Release();
            _gateHeld = false;
        }
    }
}
