namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree.Expressions
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    public sealed class DynamicExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstantDynamicExpressionReturnsProvidedValue()
        {
            Script script = new();
            DynValue constant = DynValue.NewString("constant");

            DynamicExpression expression = script.CreateConstantDynamicExpression(
                "constant",
                constant
            );

            await Assert.That(expression.IsConstant()).IsTrue().ConfigureAwait(false);
            DynValue result = expression.Evaluate();
            await Assert.That(result.String).IsEqualTo("constant").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateUsesCurrentGlobalValues()
        {
            Script script = new() { Globals = { ["x"] = 21 } };

            DynamicExpression firstExpression = script.CreateDynamicExpression("x * 2");
            DynValue result = firstExpression.Evaluate(script.CreateDynamicExecutionContext());
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);

            script.Globals["x"] = 5;
            DynamicExpression secondExpression = script.CreateDynamicExpression("x + 10");
            DynValue second = secondExpression.Evaluate(script.CreateDynamicExecutionContext());
            await Assert.That(second.Number).IsEqualTo(15d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateRespectsCustomEnvironment()
        {
            Script script = new();
            Table env = new(script) { ["value"] = DynValue.NewNumber(3) };

            DynValue function = script.LoadString("return value * 5", env, "dynamic-expression");
            DynValue result = script.Call(function);

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(15d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FindSymbolResolvesGlobalReferences()
        {
            Script script = new();
            script.Globals["foo"] = DynValue.NewNumber(123);

            DynamicExpression expression = script.CreateDynamicExpression("foo");
            SymbolRef symbol = expression.FindSymbol(script.CreateDynamicExecutionContext());

            await Assert.That(symbol).IsNotNull().ConfigureAwait(false);
            await Assert.That(symbol.Type).IsEqualTo(SymbolRefType.Global).ConfigureAwait(false);
            await Assert.That(symbol.Name).IsEqualTo("foo").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EqualityIsBasedOnExpressionCode()
        {
            Script script = new();

            DynamicExpression first = script.CreateDynamicExpression("foo");
            DynamicExpression second = script.CreateDynamicExpression("foo");
            DynamicExpression third = script.CreateDynamicExpression("bar");

            await Assert.That(first).IsEqualTo(second).ConfigureAwait(false);
            await Assert
                .That(first.GetHashCode())
                .IsEqualTo(second.GetHashCode())
                .ConfigureAwait(false);
            await Assert.That(first.Equals(third)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateThrowsWhenContextBelongsToDifferentScript()
        {
            Script owner = new();
            DynamicExpression expression = owner.CreateDynamicExpression("1");

            Script other = new();
            ScriptExecutionContext context = other.CreateDynamicExecutionContext();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                expression.Evaluate(context)
            );

            await Assert
                .That(exception.Message)
                .Contains("resource owned by a script")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FindSymbolThrowsWhenContextIsNull()
        {
            Script script = new();
            DynamicExpression expression = script.CreateDynamicExpression("foo");

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                expression.FindSymbol(null)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FindSymbolReturnsNullForConstantExpressions()
        {
            Script script = new();
            DynamicExpression constant = script.CreateConstantDynamicExpression(
                "constant",
                DynValue.NewString("value")
            );

            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            await Assert.That(constant.FindSymbol(context)).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateUsesProvidedContextForNonConstantExpressions()
        {
            Script script = new();
            script.Globals["value"] = DynValue.NewNumber(7);

            DynamicExpression expression = script.CreateDynamicExpression("value + 1");
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            DynValue result = expression.Evaluate(context);

            await Assert.That(result.Number).IsEqualTo(8d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EqualsReturnsFalseForNonDynamicExpressionInstances()
        {
            Script script = new();
            DynamicExpression expression = script.CreateDynamicExpression("foo");

            await Assert.That(expression.Equals("foo")).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetHashCodeDefaultsToZeroWhenExpressionCodeIsNull()
        {
            Script script = new();
            DynamicExpression expression = new(script, null, DynValue.NewNumber(1));

            await Assert.That(expression.GetHashCode()).IsEqualTo(0).ConfigureAwait(false);
        }
    }
}
