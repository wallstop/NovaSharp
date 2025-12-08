namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Core.Enums;
    using global::TUnit.Core.Interfaces;
    using WallstopStudios.NovaSharp.Interpreter;

    /// <summary>
    /// Ensures that tests modifying <see cref="Script.DefaultOptions"/> are serialized
    /// and that the default options are restored after each test.
    /// This prevents race conditions where one test's changes to <see cref="Script.DefaultOptions"/>
    /// affect other tests running in parallel.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true
    )]
    public sealed class ScriptDefaultOptionsIsolationAttribute
        : Attribute,
            ITestStartEventReceiver,
            ITestEndEventReceiver
    {
        private const string ScopeKey = "NovaSharp.Tests.ScriptDefaultOptionsIsolation.Scope";
        private const string GateKey = "NovaSharp.Tests.ScriptDefaultOptionsIsolation.Gate";
        private static readonly SemaphoreSlim IsolationGate = new(1, 1);

        public EventReceiverStage Stage => EventReceiverStage.Late;

        public ValueTask OnTestStart(global::TUnit.Core.TestContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return OnTestStartInternal(context);
        }

        private static async ValueTask OnTestStartInternal(global::TUnit.Core.TestContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            await IsolationGate.WaitAsync().ConfigureAwait(false);
            context.StateBag[GateKey] = true;
            context.StateBag[ScopeKey] = Script.BeginDefaultOptionsScope();
        }

        public ValueTask OnTestEnd(global::TUnit.Core.TestContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (
                context.StateBag.TryRemove(ScopeKey, out object value) && value is IDisposable scope
            )
            {
                scope.Dispose();
            }

            if (context.StateBag.TryRemove(GateKey, out _))
            {
                IsolationGate.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}
