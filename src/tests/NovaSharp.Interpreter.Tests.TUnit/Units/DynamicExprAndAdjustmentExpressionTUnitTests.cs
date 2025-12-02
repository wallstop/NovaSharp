namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.Units;
    using NovaSharp.Interpreter.Tree;
    using NovaSharp.Interpreter.Tree.Expressions;

    public sealed class DynamicExprExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task EvalDelegatesAndMarksContextAnonymous()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            SymbolRef dynamicRef = SymbolRef.Global("value", SymbolRef.DefaultEnv);
            StubExpression inner = new(context, DynValue.NewNumber(7), dynamicRef);
            DynamicExprExpression expression = new(inner, context);
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            DynValue result = expression.Eval(executionContext);

            await Assert.That(context.Anonymous).IsTrue().ConfigureAwait(false);
            await Assert.That(inner.EvalCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(7).ConfigureAwait(false);
            await Assert
                .That(expression.FindDynamic(executionContext))
                .IsEqualTo(dynamicRef)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CompileThrowsInvalidOperation()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            DynamicExprExpression expression = new(
                new StubExpression(context, DynValue.NewNumber(1)),
                context
            );

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                expression.Compile(new ByteCode(script))
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }
    }

    public sealed class AdjustmentExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task EvalReturnsScalarValue()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(5), DynValue.NewNumber(9));
            AdjustmentExpression expression = new(context, new StubExpression(context, tuple));
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            DynValue result = expression.Eval(executionContext);

            await Assert.That(result.Number).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CompileEmitsScalarInstruction()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            StubExpression inner = new(context, DynValue.NewNumber(3));
            AdjustmentExpression expression = new(context, inner);
            ByteCode byteCode = new(script);

            expression.Compile(byteCode);

            await Assert.That(inner.CompileCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(byteCode.Code[^1].OpCode)
                .IsEqualTo(OpCode.Scalar)
                .ConfigureAwait(false);
        }
    }

    internal sealed class StubExpression : Expression
    {
        private readonly DynValue _value;
        private readonly SymbolRef _dynamic;

        internal StubExpression(
            ScriptLoadingContext context,
            DynValue value,
            SymbolRef dynamicRef = null
        )
            : base(context)
        {
            _value = value ?? DynValue.Void;
            _dynamic = dynamicRef;
        }

        internal int CompileCount { get; private set; }

        internal int EvalCount { get; private set; }

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
}
