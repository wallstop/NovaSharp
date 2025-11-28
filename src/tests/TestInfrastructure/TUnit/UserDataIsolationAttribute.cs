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
    internal sealed class UserDataIsolationAttribute
        : Attribute,
            ITestStartEventReceiver,
            ITestEndEventReceiver
    {
        private readonly AsyncLocal<IDisposable> _scope = new();

        public EventReceiverStage Stage => EventReceiverStage.Early;

        public ValueTask OnTestStart(global::TUnit.Core.TestContext context)
        {
            _scope.Value = UserData.BeginIsolationScope();
            return ValueTask.CompletedTask;
        }

        public ValueTask OnTestEnd(global::TUnit.Core.TestContext context)
        {
            _scope.Value?.Dispose();
            _scope.Value = null;
            return ValueTask.CompletedTask;
        }
    }
}
