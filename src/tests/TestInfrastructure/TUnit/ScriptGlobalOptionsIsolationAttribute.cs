namespace NovaSharp.Interpreter.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Core.Enums;
    using global::TUnit.Core.Interfaces;
    using NovaSharp.Interpreter;

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true
    )]
    public sealed class ScriptGlobalOptionsIsolationAttribute
        : Attribute,
            ITestStartEventReceiver,
            ITestEndEventReceiver
    {
        private const string ScopeKey = "NovaSharp.Tests.ScriptGlobalOptionsIsolation.Scope";
        private const string GateKey = "NovaSharp.Tests.ScriptGlobalOptionsIsolation.Gate";
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
            context.StateBag[ScopeKey] = Script.BeginGlobalOptionsScope();
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
