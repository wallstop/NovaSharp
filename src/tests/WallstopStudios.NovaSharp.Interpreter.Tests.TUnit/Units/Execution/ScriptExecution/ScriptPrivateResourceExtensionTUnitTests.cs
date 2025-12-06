namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    public sealed class ScriptPrivateResourceExtensionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CheckScriptOwnershipAllowsResourcesOwnedBySameScript()
        {
            Script script = new();
            TestResource container = new(script);
            DynValue dynValue = DynValue.NewTable(script);

            container.CheckScriptOwnership(dynValue);
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckScriptOwnershipThrowsForDifferentScripts()
        {
            Script scriptA = new();
            Script scriptB = new();
            TestResource container = new(scriptA);
            DynValue dynValue = DynValue.NewTable(scriptB);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                container.CheckScriptOwnership(dynValue)
            );

            await Assert.That(exception.Message).Contains("resources owned by different scripts");
        }

        [global::TUnit.Core.Test]
        public async Task CheckScriptOwnershipThrowsWhenSharedReceivesPrivate()
        {
            TestResource sharedContainer = new(owner: null);
            DynValue dynValue = DynValue.NewTable(new Script());

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                sharedContainer.CheckScriptOwnership(dynValue)
            );

            await Assert
                .That(exception.Message)
                .Contains("script private resource on a shared resource");
        }

        [global::TUnit.Core.Test]
        public async Task CheckScriptOwnershipValidatesEveryValueInArray()
        {
            Script scriptA = new();
            Script scriptB = new();
            TestResource container = new(scriptA);
            DynValue[] values = { DynValue.NewTable(scriptA), DynValue.NewTable(scriptB) };

            ExpectException<ScriptRuntimeException>(() => container.CheckScriptOwnership(values));
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckScriptOwnershipIgnoresNullDynValues()
        {
            TestResource container = new(new Script());
            container.CheckScriptOwnership((DynValue)null);
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckScriptOwnershipAllowsNonPrivateValues()
        {
            TestResource container = new(new Script());
            DynValue constant = DynValue.NewNumber(123);

            container.CheckScriptOwnership(constant);
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckScriptOwnershipGuardsScriptParameter()
        {
            Script owner = new();
            Script other = new();
            TestResource container = new(owner);

            container.CheckScriptOwnership(owner);
            container.CheckScriptOwnership(script: null);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                container.CheckScriptOwnership(other)
            );

            await Assert.That(exception.Message).Contains("another script");
        }

        private sealed class TestResource : IScriptPrivateResource
        {
            public TestResource(Script owner)
            {
                OwnerScript = owner;
            }

            public Script OwnerScript { get; }
        }

        private static TException ExpectException<TException>(System.Action action)
            where TException : System.Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new System.InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }
}
