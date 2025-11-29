namespace NovaSharp.Interpreter.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Core.Enums;
    using global::TUnit.Core.Interfaces;
    using NovaSharp.Interpreter.DataTypes;

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true
    )]
    public sealed class UserDataIsolationAttribute
        : Attribute,
            ITestStartEventReceiver,
            ITestEndEventReceiver
    {
        private const string ScopeKey = "NovaSharp.Tests.UserDataIsolation.Scope";
        private const string GateKey = "NovaSharp.Tests.UserDataIsolation.Gate";

        // Serialize user-data heavy fixtures until the TUnit worker lifecycle guarantees per-test isolation.
        private static readonly SemaphoreSlim IsolationGate = new(1, 1);

        public EventReceiverStage Stage => EventReceiverStage.Late;

        public async ValueTask OnTestStart(global::TUnit.Core.TestContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            await IsolationGate.WaitAsync().ConfigureAwait(false);
            context.StateBag[GateKey] = true;
            context.StateBag[ScopeKey] = UserData.BeginIsolationScope();
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
