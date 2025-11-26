namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using Execution;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public class DynamicExpressionTests
    {
        [Test]
        public void ConstantDynamicExpressionReturnsProvidedValue()
        {
            Script script = new();
            DynValue constant = DynValue.NewString("constant");

            DynamicExpression expression = script.CreateConstantDynamicExpression(
                "constant",
                constant
            );

            Assert.Multiple(() =>
            {
                Assert.That(expression.IsConstant(), Is.True);
                Assert.That(expression.Evaluate().String, Is.EqualTo("constant"));
            });
        }

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

        [Test]
        public void FindSymbolResolvesGlobalReferences()
        {
            Script script = new();
            script.Globals["foo"] = DynValue.NewNumber(123);

            DynamicExpression expression = script.CreateDynamicExpression("foo");
            SymbolRef symbol = expression.FindSymbol(script.CreateDynamicExecutionContext());

            Assert.Multiple(() =>
            {
                Assert.That(symbol, Is.Not.Null);
                Assert.That(symbol.Type, Is.EqualTo(SymbolRefType.Global));
                Assert.That(symbol.Name, Is.EqualTo("foo"));
            });
        }

        [Test]
        public void EqualityIsBasedOnExpressionCode()
        {
            Script script = new();

            DynamicExpression first = script.CreateDynamicExpression("foo");
            DynamicExpression second = script.CreateDynamicExpression("foo");
            DynamicExpression third = script.CreateDynamicExpression("bar");

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo(second));
                Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
                Assert.That(first.Equals(third), Is.False);
            });
        }

        [Test]
        public void EvaluateThrowsWhenContextBelongsToDifferentScript()
        {
            Script owner = new();
            DynamicExpression expression = owner.CreateDynamicExpression("1");

            Script other = new();
            ScriptExecutionContext context = other.CreateDynamicExecutionContext();

            Assert.That(
                () => expression.Evaluate(context),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contain("resource owned by a script")
            );
        }

        [Test]
        public void FindSymbolThrowsWhenContextIsNull()
        {
            Script script = new();
            DynamicExpression expression = script.CreateDynamicExpression("foo");

            Assert.That(() => expression.FindSymbol(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void FindSymbolReturnsNullForConstantExpressions()
        {
            Script script = new();
            DynamicExpression constant = script.CreateConstantDynamicExpression(
                "constant",
                DynValue.NewString("value")
            );

            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Assert.That(constant.FindSymbol(context), Is.Null);
        }

        [Test]
        public void EvaluateUsesProvidedContextForNonConstantExpressions()
        {
            Script script = new();
            script.Globals["value"] = DynValue.NewNumber(7);

            DynamicExpression expression = script.CreateDynamicExpression("value + 1");
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            DynValue result = expression.Evaluate(context);

            Assert.That(result.Number, Is.EqualTo(8));
        }

        [Test]
        public void EqualsReturnsFalseForNonDynamicExpressionInstances()
        {
            Script script = new();
            DynamicExpression expression = script.CreateDynamicExpression("foo");

            Assert.That(expression.Equals("foo"), Is.False);
        }

        [Test]
        public void GetHashCodeDefaultsToZeroWhenExpressionCodeIsNull()
        {
            Script script = new();
            DynamicExpression expression = new(script, null, DynValue.NewNumber(1));

            Assert.That(expression.GetHashCode(), Is.EqualTo(0));
        }
    }
}
