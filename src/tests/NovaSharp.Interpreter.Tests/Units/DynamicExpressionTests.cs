namespace NovaSharp.Interpreter.Tests.Units
{
    using NUnit.Framework;

    [TestFixture]
    public class DynamicExpressionTests
    {
        [Test]
        public void EvaluateUsesCurrentGlobalValues()
        {
            Script script = new() { Globals = { ["x"] = 21 } };

            DynValue result = script.Evaluate("x * 2");
            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(42));
            });

            script.Globals["x"] = 5;
            DynValue second = script.Evaluate("x + 10");
            Assert.That(second.Number, Is.EqualTo(15));
        }

        [Test]
        public void EvaluateRespectsCustomEnvironment()
        {
            Script script = new();
            Table env = new(script) { ["value"] = DynValue.NewNumber(3) };

            DynValue function = script.LoadString("return value * 5", env, "dynamic-expression");
            DynValue result = script.Call(function);

            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(15));
        }
    }
}
