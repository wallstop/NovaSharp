namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree.Expressions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;
    using WallstopStudios.NovaSharp.Interpreter.Tree;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Expressions;

    public sealed class ExprListExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CompileEmitsTupleWhenMultipleExpressions()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            StubExpression first = new(context, DynValue.NewNumber(1));
            StubExpression second = new(context, DynValue.NewNumber(2));
            ExprListExpression list = new(new List<Expression> { first, second }, context);
            ByteCode byteCode = new(script);

            list.Compile(byteCode);

            await Assert.That(first.CompileCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(second.CompileCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(byteCode.Code[^1].OpCode)
                .IsEqualTo(OpCode.MkTuple)
                .ConfigureAwait(false);
            await Assert.That(byteCode.Code[^1].NumVal).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CompileSkipsTupleWhenSingleExpression()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            StubExpression expression = new(context, DynValue.NewNumber(42));
            ExprListExpression list = new(new List<Expression> { expression }, context);
            ByteCode byteCode = new(script);

            list.Compile(byteCode);

            await Assert.That(expression.CompileCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(byteCode.Code[^1].OpCode).IsEqualTo(OpCode.Nop).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvalReturnsFirstExpressionValue()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            DynValue expected = DynValue.NewString("primary");
            StubExpression first = new(context, expected);
            StubExpression second = new(context, DynValue.NewString("secondary"));
            ExprListExpression list = new(new List<Expression> { first, second }, context);
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            DynValue result = list.Eval(executionContext);

            await Assert.That(result).IsEqualTo(expected).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EvalReturnsVoidWhenListIsEmpty()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            ExprListExpression list = new(new List<Expression>(), context);
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            DynValue result = list.Eval(executionContext);

            await Assert.That(result).IsEqualTo(DynValue.Void).ConfigureAwait(false);
        }

        private sealed class StubExpression : Expression
        {
            private readonly DynValue _value;

            public StubExpression(ScriptLoadingContext context, DynValue value)
                : base(context)
            {
                _value = value;
            }

            public int CompileCount { get; private set; }

            public override void Compile(ByteCode bc)
            {
                CompileCount++;
                bc.EmitNop("stub");
            }

            public override DynValue Eval(ScriptExecutionContext context)
            {
                return _value;
            }
        }
    }
}
