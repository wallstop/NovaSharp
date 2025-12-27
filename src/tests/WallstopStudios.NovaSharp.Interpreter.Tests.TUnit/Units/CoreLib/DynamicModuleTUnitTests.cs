namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.CoreLib
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class DynamicModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task EvalExecutesExpressionAgainstCurrentGlobals(
            LuaCompatibilityVersion version
        )
        {
            using UserDataRegistrationScope registrationScope = RegisterDummyType();
            Script script = new(version, CoreModulePresets.Complete);
            script.Globals["value"] = DynValue.NewNumber(6);

            DynValue result = script.DoString("return dynamic.eval('value * 3')");

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(18d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PreparedExpressionCanBeEvaluatedMultipleTimes(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            DynValue prepared = script.DoString("return dynamic.prepare('a + b')");
            script.Globals["expr"] = prepared;

            script.Globals["a"] = DynValue.NewNumber(2);
            script.Globals["b"] = DynValue.NewNumber(3);
            DynValue first = script.DoString("return dynamic.eval(expr)");

            script.Globals["a"] = DynValue.NewNumber(10);
            script.Globals["b"] = DynValue.NewNumber(-4);
            DynValue second = script.DoString("return dynamic.eval(expr)");

            await Assert.That(first.Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(6d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task EvalThrowsWhenUserDataIsNotPreparedExpression(
            LuaCompatibilityVersion version
        )
        {
            using UserDataRegistrationScope registrationScope = RegisterDummyType();
            Script script = new(version, CoreModulePresets.Complete);
            script.Globals["bad"] = UserData.Create(new Dummy());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return dynamic.eval(bad)")
            )!;

            await Assert
                .That(exception.Message)
                .Contains("was not a previously prepared expression")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task EvalThrowsScriptRuntimeExceptionOnSyntaxError(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return dynamic.eval('function(')")
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PrepareThrowsScriptRuntimeExceptionOnSyntaxError(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return dynamic.prepare('function(')")
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        private static UserDataRegistrationScope RegisterDummyType()
        {
            UserDataRegistrationScope scope = UserDataRegistrationScope.Track<Dummy>(
                ensureUnregistered: true
            );
            scope.RegisterType<Dummy>();
            return scope;
        }

        private sealed class Dummy { }
    }
}
