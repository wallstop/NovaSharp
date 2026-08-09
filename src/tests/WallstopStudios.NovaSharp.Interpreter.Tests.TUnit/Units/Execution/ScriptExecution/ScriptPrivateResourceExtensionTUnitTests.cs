namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ScriptPrivateResourceExtensionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CheckScriptOwnershipAllowsResourcesOwnedBySameScript()
        {
            Script script = new();
            TestResource container = new(script);
            LuaValue dynValue = LuaValue.NewTable(script);

            container.CheckScriptOwnership(dynValue);
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckScriptOwnershipThrowsForDifferentScripts()
        {
            Script scriptA = new();
            Script scriptB = new();
            TestResource container = new(scriptA);
            LuaValue dynValue = LuaValue.NewTable(scriptB);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                container.CheckScriptOwnership(dynValue)
            );

            await Assert.That(exception.Message).Contains("resources owned by different scripts");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CheckScriptOwnershipThrowsWhenSharedReceivesPrivate(
            LuaCompatibilityVersion version
        )
        {
            TestResource sharedContainer = new(owner: null);
            LuaValue dynValue = LuaValue.NewTable(new Script(version));

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
            LuaValue[] values = { LuaValue.NewTable(scriptA), LuaValue.NewTable(scriptB) };

            ExpectException<ScriptRuntimeException>(() => container.CheckScriptOwnership(values));
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckScriptOwnershipRejectsForeignResourcesNestedInTuples()
        {
            Script scriptA = new();
            Script scriptB = new();
            TestResource container = new(scriptA);
            LuaValue nested = LuaValue.NewTuple(
                LuaValue.NewNumber(1),
                LuaValue.NewTuple(LuaValue.NewString("value"), LuaValue.NewTable(scriptB))
            );
            for (int i = 0; i < 4_096; i++)
            {
                nested = LuaValue.NewTuple(nested);
            }

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                container.CheckScriptOwnership(nested)
            );

            await Assert.That(exception.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CheckScriptOwnershipIgnoresNullDynValues(LuaCompatibilityVersion version)
        {
            TestResource container = new(new Script(version));
            container.CheckScriptOwnership(LuaValue.Nil);
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CheckScriptOwnershipAllowsNonPrivateValues(
            LuaCompatibilityVersion version
        )
        {
            TestResource container = new(new Script(version));
            LuaValue constant = LuaValue.NewNumber(123);

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
