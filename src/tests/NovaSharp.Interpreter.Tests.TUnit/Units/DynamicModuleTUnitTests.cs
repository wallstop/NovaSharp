namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;

    public sealed class DynamicModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task EvalExecutesExpressionAgainstCurrentGlobals()
        {
            EnsureDummyRegistered();
            Script script = new(CoreModules.PresetComplete);
            script.Globals["value"] = DynValue.NewNumber(6);

            DynValue result = script.DoString("return dynamic.eval('value * 3')");

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(18d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PreparedExpressionCanBeEvaluatedMultipleTimes()
        {
            Script script = new(CoreModules.PresetComplete);

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
        public async Task EvalThrowsWhenUserDataIsNotPreparedExpression()
        {
            EnsureDummyRegistered();
            Script script = new(CoreModules.PresetComplete);
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
        public async Task EvalThrowsScriptRuntimeExceptionOnSyntaxError()
        {
            Script script = new(CoreModules.PresetComplete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return dynamic.eval('function(')")
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PrepareThrowsScriptRuntimeExceptionOnSyntaxError()
        {
            Script script = new(CoreModules.PresetComplete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return dynamic.prepare('function(')")
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        private static void EnsureDummyRegistered()
        {
            if (!UserData.IsTypeRegistered<Dummy>())
            {
                UserData.RegisterType<Dummy>();
            }
        }

        private sealed class Dummy { }
    }
}
