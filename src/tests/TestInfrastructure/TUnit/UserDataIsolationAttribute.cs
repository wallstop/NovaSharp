namespace NovaSharp.Interpreter.Tests
{
    using System;
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

        public EventReceiverStage Stage => EventReceiverStage.Late;

        public ValueTask OnTestStart(global::TUnit.Core.TestContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            context.StateBag[ScopeKey] = UserData.BeginIsolationScope();
            return ValueTask.CompletedTask;
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

            return ValueTask.CompletedTask;
        }
    }
}
