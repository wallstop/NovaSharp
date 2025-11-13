namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;
    using Expression = NovaSharp.Interpreter.Tree.Expression;

    [TestFixture]
    public sealed class BinaryOperatorExpressionTests
    {
        [Test]
        public void OrShortCircuitsWhenFirstOperandIsTruthy()
        {
            Script script = new Script();
            StubExpression rhsStub = null!;

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.Or,
                "or",
                ctx => new LiteralExpression(ctx, DynValue.True),
                ctx =>
                {
                    rhsStub = new StubExpression(ctx, _ => DynValue.True);
                    return rhsStub;
                }
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.Multiple(() =>
            {
                Assert.That(result.Boolean, Is.True);
                Assert.That(rhsStub, Is.Not.Null);
                Assert.That(rhsStub.EvalCount, Is.Zero);
            });
        }

        [Test]
        public void OrEvaluatesSecondOperandWhenFirstIsFalsey()
        {
            Script script = new Script();
            StubExpression rhsStub = null!;

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.Or,
                "or",
                ctx => new LiteralExpression(ctx, DynValue.Nil),
                ctx =>
                {
                    rhsStub = new StubExpression(ctx, _ => DynValue.NewNumber(42));
                    return rhsStub;
                }
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.Multiple(() =>
            {
                Assert.That(result.Number, Is.EqualTo(42));
                Assert.That(rhsStub, Is.Not.Null);
                Assert.That(rhsStub.EvalCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void AndShortCircuitsWhenFirstOperandIsFalsey()
        {
            Script script = new Script();
            StubExpression rhsStub = null!;

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.And,
                "and",
                ctx => new LiteralExpression(ctx, DynValue.Nil),
                ctx =>
                {
                    rhsStub = new StubExpression(ctx, _ => DynValue.NewNumber(99));
                    return rhsStub;
                }
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsNil(), Is.True);
                Assert.That(rhsStub, Is.Not.Null);
                Assert.That(rhsStub.EvalCount, Is.Zero);
            });
        }

        [Test]
        public void AndEvaluatesSecondOperandWhenFirstIsTruthy()
        {
            Script script = new Script();
            StubExpression rhsStub = null!;

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.And,
                "and",
                ctx => new LiteralExpression(ctx, DynValue.True),
                ctx =>
                {
                    rhsStub = new StubExpression(ctx, _ => DynValue.NewNumber(1337));
                    return rhsStub;
                }
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.Multiple(() =>
            {
                Assert.That(result.Number, Is.EqualTo(1337));
                Assert.That(rhsStub, Is.Not.Null);
                Assert.That(rhsStub.EvalCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void ArithmeticOperationsReturnExpectedResult()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpAdd,
                "+",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(4)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(6))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Number, Is.EqualTo(10));
        }

        [Test]
        public void ModuloProducesPositiveResult()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpMod,
                "%",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(-3)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Number, Is.EqualTo(1));
        }

        [Test]
        public void ArithmeticThrowsOnNonNumericOperands()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpMul,
                "*",
                ctx => new LiteralExpression(ctx, DynValue.NewString("a")),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            Assert.That(
                () => expr.Eval(TestHelpers.CreateExecutionContext(script)),
                Throws.TypeOf<DynamicExpressionException>().With.Message.Contains("arithmetic")
            );
        }

        [Test]
        public void ConcatenationCombinesStrings()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpConcat,
                "..",
                ctx => new LiteralExpression(ctx, DynValue.NewString("Lua")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("Sharp"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.String, Is.EqualTo("LuaSharp"));
        }

        [Test]
        public void ConcatenationThrowsForNonStringOperands()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpConcat,
                "..",
                ctx => new LiteralExpression(ctx, DynValue.NewString("Lua")),
                ctx => new LiteralExpression(ctx, DynValue.NewTable(ctx.Script))
            );

            Assert.That(
                () => expr.Eval(TestHelpers.CreateExecutionContext(script)),
                Throws.TypeOf<DynamicExpressionException>().With.Message.Contains("concatenation")
            );
        }

        [Test]
        public void ComparisonEvaluatesNumericOrdering()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(3)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(7))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void ComparisonSupportsGreaterThan()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpGreaterThan,
                ">",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(9)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void EqualityTreatsNilAndVoidAsEqual()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpEqual,
                "==",
                ctx => new LiteralExpression(ctx, DynValue.Nil),
                ctx => new LiteralExpression(ctx, DynValue.Void)
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void ComparisonThrowsForMismatchedTypes()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(1)),
                ctx => new LiteralExpression(ctx, DynValue.NewString("x"))
            );

            Assert.That(
                () => expr.Eval(TestHelpers.CreateExecutionContext(script)),
                Throws.TypeOf<DynamicExpressionException>().With.Message.Contains("compare")
            );
        }

        private static Expression BuildBinaryExpression(
            Script script,
            TokenType tokenType,
            string tokenText,
            Func<ScriptLoadingContext, Expression> leftFactory,
            Func<ScriptLoadingContext, Expression> rightFactory
        )
        {
            ScriptLoadingContext ctx = new(script);
            object chain = BinaryOperatorExpression.BeginOperatorChain();
            BinaryOperatorExpression.AddExpressionToChain(chain, leftFactory(ctx));
            BinaryOperatorExpression.AddOperatorToChain(chain, CreateToken(tokenType, tokenText));
            BinaryOperatorExpression.AddExpressionToChain(chain, rightFactory(ctx));
            return BinaryOperatorExpression.CommitOperatorChain(chain, ctx);
        }

        private static Token CreateToken(TokenType type, string text)
        {
            return new Token(type, 0, 0, 0, 0, 0, 0, 0) { Text = text };
        }

        private sealed class StubExpression : Expression
        {
            private readonly Func<ScriptExecutionContext, DynValue> evaluate;

            public StubExpression(
                ScriptLoadingContext context,
                Func<ScriptExecutionContext, DynValue> evaluate
            )
                : base(context)
            {
                this.evaluate = evaluate;
            }

            public int EvalCount { get; private set; }

            public override void Compile(ByteCode bc)
            {
                // No-op for coverage purposes; tests exercise Eval directly.
            }

            public override DynValue Eval(ScriptExecutionContext context)
            {
                EvalCount++;
                return evaluate(context);
            }
        }
    }
}
