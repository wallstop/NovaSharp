namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DynamicExprExpressionTests
    {
        [Test]
        public void EvalDelegatesAndMarksContextAnonymous()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            SymbolRef dynamicRef = SymbolRef.Global("value", SymbolRef.DefaultEnv);
            StubExpression inner = new(context, DynValue.NewNumber(7), dynamicRef);
            DynamicExprExpression expression = new(inner, context);
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            DynValue result = ExpressionTestHelpers.InvokeEval(expression, executionContext);

            Assert.Multiple(() =>
            {
                Assert.That(context.Anonymous, Is.True);
                Assert.That(inner.EvalCount, Is.EqualTo(1));
                Assert.That(result.Number, Is.EqualTo(7));
                Assert.That(expression.FindDynamic(executionContext), Is.EqualTo(dynamicRef));
            });
        }

        [Test]
        public void CompileThrowsInvalidOperation()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            DynamicExprExpression expression = new(
                new StubExpression(context, DynValue.NewNumber(1)),
                context
            );

            Assert.That(
                () => ExpressionTestHelpers.InvokeCompile(expression, new ByteCode(script)),
                Throws.InvalidOperationException
            );
        }
    }

    [TestFixture]
    public sealed class AdjustmentExpressionTests
    {
        [Test]
        public void EvalReturnsScalarValue()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(5), DynValue.NewNumber(9));
            AdjustmentExpression expression = new(context, new StubExpression(context, tuple));
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            DynValue result = ExpressionTestHelpers.InvokeEval(expression, executionContext);

            Assert.That(result.Number, Is.EqualTo(5));
        }

        [Test]
        public void CompileEmitsScalarInstruction()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            StubExpression inner = new(context, DynValue.NewNumber(3));
            AdjustmentExpression expression = new(context, inner);
            ByteCode byteCode = new(script);

            ExpressionTestHelpers.InvokeCompile(expression, byteCode);

            Assert.Multiple(() =>
            {
                Assert.That(inner.CompileCount, Is.EqualTo(1));
                Assert.That(byteCode.Code[^1].OpCode, Is.EqualTo(OpCode.Scalar));
            });
        }
    }

    internal sealed class StubExpression : Expression
    {
        private readonly DynValue _value;
        private readonly SymbolRef _dynamic;

        public StubExpression(
            ScriptLoadingContext context,
            DynValue value,
            SymbolRef dynamicRef = null
        )
            : base(context)
        {
            _value = value ?? DynValue.Void;
            _dynamic = dynamicRef;
        }

        public int CompileCount { get; private set; }

        public int EvalCount { get; private set; }

        public override void Compile(ByteCode bc)
        {
            CompileCount++;
            bc.EmitNop("stub");
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            EvalCount++;
            return _value;
        }

        public override SymbolRef FindDynamic(ScriptExecutionContext context)
        {
            return _dynamic;
        }
    }

    internal static class ExpressionTestHelpers
    {
        public static DynValue InvokeEval(Expression expression, ScriptExecutionContext context)
        {
            MethodInfo method = expression
                .GetType()
                .GetMethod(
                    nameof(Expression.Eval),
                    BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly
                );

            if (method == null)
            {
                throw new InvalidOperationException("Eval override not found on expression.");
            }

            try
            {
                return (DynValue)method.Invoke(expression, new object[] { context });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        public static void InvokeCompile(Expression expression, ByteCode byteCode)
        {
            MethodInfo method = expression
                .GetType()
                .GetMethod(
                    nameof(Expression.Compile),
                    BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly
                );

            if (method == null)
            {
                throw new InvalidOperationException("Compile override not found on expression.");
            }

            try
            {
                method.Invoke(expression, new object[] { byteCode });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }
    }
}
