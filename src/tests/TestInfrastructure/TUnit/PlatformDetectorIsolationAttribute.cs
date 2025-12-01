namespace NovaSharp.Interpreter.Tests
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Core.Enums;
    using global::TUnit.Core.Interfaces;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true
    )]
    public sealed class PlatformDetectorIsolationAttribute
        : Attribute,
            ITestStartEventReceiver,
            ITestEndEventReceiver
    {
        private const string ScopeKey = "NovaSharp.Tests.PlatformDetectorIsolation.Scope";

        public EventReceiverStage Stage => EventReceiverStage.Early;

        public async ValueTask OnTestStart(global::TUnit.Core.TestContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.StateBag[ScopeKey] = await PlatformDetectorIsolationScope
                .EnterAsync()
                .ConfigureAwait(false);
        }

        public async ValueTask OnTestEnd(global::TUnit.Core.TestContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (
                context.StateBag.TryRemove(ScopeKey, out object value)
                && value is PlatformDetectorIsolationScope scope
            )
            {
                await scope.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
