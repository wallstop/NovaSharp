namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DynamicModuleTests
    {
        [OneTimeSetUp]
        public void RegisterDummy()
        {
            if (!UserData.IsTypeRegistered<Dummy>())
            {
                UserData.RegisterType<Dummy>();
            }
        }

        [Test]
        public void EvalExecutesExpressionAgainstCurrentGlobals()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Globals["value"] = DynValue.NewNumber(6);

            DynValue result = script.DoString("return dynamic.eval('value * 3')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(18d));
            });
        }

        [Test]
        public void PreparedExpressionCanBeEvaluatedMultipleTimes()
        {
            Script script = new Script(CoreModules.PresetComplete);

            DynValue prepared = script.DoString("return dynamic.prepare('a + b')");
            script.Globals["expr"] = prepared;

            script.Globals["a"] = DynValue.NewNumber(2);
            script.Globals["b"] = DynValue.NewNumber(3);
            DynValue first = script.DoString("return dynamic.eval(expr)");

            script.Globals["a"] = DynValue.NewNumber(10);
            script.Globals["b"] = DynValue.NewNumber(-4);
            DynValue second = script.DoString("return dynamic.eval(expr)");

            Assert.Multiple(() =>
            {
                Assert.That(first.Number, Is.EqualTo(5d));
                Assert.That(second.Number, Is.EqualTo(6d));
            });
        }

        [Test]
        public void EvalThrowsWhenUserDataIsNotPreparedExpression()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Globals["bad"] = UserData.Create(new Dummy());

            Assert.That(
                () => script.DoString("return dynamic.eval(bad)"),
                Throws
                    .InstanceOf<ScriptRuntimeException>()
                    .With.Message.Contains("was not a previously prepared expression")
            );
        }

        [Test]
        public void EvalThrowsScriptRuntimeExceptionOnSyntaxError()
        {
            Script script = new Script(CoreModules.PresetComplete);

            Assert.That(
                () => script.DoString("return dynamic.eval('function(')"),
                Throws.InstanceOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void PrepareThrowsScriptRuntimeExceptionOnSyntaxError()
        {
            Script script = new Script(CoreModules.PresetComplete);

            Assert.That(
                () => script.DoString("return dynamic.prepare('function(')"),
                Throws.InstanceOf<ScriptRuntimeException>()
            );
        }

        private sealed class Dummy { }
    }
}
