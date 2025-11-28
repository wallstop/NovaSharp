namespace NovaSharp.Interpreter.Tests
{
    using System;
    using System.Threading;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;

    /// <summary>
    /// Serializes access to PlatformAutoDetector state so fixtures that mutate the static flags can run in parallel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class PlatformDetectorIsolationAttribute : NUnitAttribute, ITestAction
    {
        private static readonly SemaphoreSlim IsolationGate = new(1, 1);
        private PlatformAutoDetector.PlatformDetectorSnapshot _snapshot;
        private bool _gateHeld;

        public void BeforeTest(ITest test)
        {
            IsolationGate.Wait();
            _gateHeld = true;
            _snapshot = PlatformAutoDetector.TestHooks.CaptureState();
        }

        public void AfterTest(ITest test)
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
        }

        public ActionTargets Targets => ActionTargets.Test;
    }
}
