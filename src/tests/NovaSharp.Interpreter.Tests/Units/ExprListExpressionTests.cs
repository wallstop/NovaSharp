namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ExprListExpressionTests
    {
        [Test]
        public void CompileEmitsTupleWhenMultipleExpressions()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            StubExpression first = new(context, DynValue.NewNumber(1));
            StubExpression second = new(context, DynValue.NewNumber(2));
            ExprListExpression list = new(
                new List<Expression> { first, second },
                context
            );
            ByteCode byteCode = new(script);

            list.Compile(byteCode);

            Assert.Multiple(() =>
            {
                Assert.That(first.CompileCount, Is.EqualTo(1));
                Assert.That(second.CompileCount, Is.EqualTo(1));
                Assert.That(byteCode.code[^1].OpCode, Is.EqualTo(OpCode.MkTuple));
                Assert.That(byteCode.code[^1].NumVal, Is.EqualTo(2));
            });
        }

        [Test]
        public void CompileSkipsTupleWhenSingleExpression()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            StubExpression expression = new(context, DynValue.NewNumber(42));
            ExprListExpression list = new(
                new List<Expression> { expression },
                context
            );
            ByteCode byteCode = new(script);

            list.Compile(byteCode);

            Assert.Multiple(() =>
            {
                Assert.That(expression.CompileCount, Is.EqualTo(1));
                Assert.That(byteCode.code[^1].OpCode, Is.EqualTo(OpCode.Nop));
            });
        }

        [Test]
        public void EvalReturnsFirstExpressionValue()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            DynValue expected = DynValue.NewString("primary");
            StubExpression first = new(context, expected);
            StubExpression second = new(context, DynValue.NewString("secondary"));
            ExprListExpression list = new(
                new List<Expression> { first, second },
                context
            );
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            DynValue result = list.Eval(executionContext);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void EvalReturnsVoidWhenListIsEmpty()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            ExprListExpression list = new(new List<Expression>(), context);
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            DynValue result = list.Eval(executionContext);

            Assert.That(result, Is.EqualTo(DynValue.Void));
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
                bc.Emit_Nop("stub");
            }

            public override DynValue Eval(ScriptExecutionContext context)
            {
                return _value;
            }
        }
    }
}
