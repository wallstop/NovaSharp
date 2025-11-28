namespace NovaSharp.Interpreter.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Core.Enums;
    using global::TUnit.Core.Interfaces;
    using NovaSharp.Interpreter.Platforms;

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true
    )]
    internal sealed class PlatformDetectorIsolationAttribute
        : Attribute,
            ITestStartEventReceiver,
            ITestEndEventReceiver
    {
        private static readonly SemaphoreSlim IsolationGate = new(1, 1);
        private PlatformAutoDetector.PlatformDetectorSnapshot _snapshot;
        private bool _gateHeld;

        public EventReceiverStage Stage => EventReceiverStage.Early;

        public async ValueTask OnTestStart(global::TUnit.Core.TestContext context)
        {
            await IsolationGate.WaitAsync().ConfigureAwait(false);
            _gateHeld = true;
            _snapshot = PlatformAutoDetector.TestHooks.CaptureState();
        }

        public ValueTask OnTestEnd(global::TUnit.Core.TestContext context)
        {
            try
            {
                if (_snapshot != null)
                {
                    PlatformAutoDetector.TestHooks.RestoreState(_snapshot);
                    _snapshot = null;
                }
            }
            finally
            {
                if (_gateHeld)
                {
                    IsolationGate.Release();
                    _gateHeld = false;
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}
