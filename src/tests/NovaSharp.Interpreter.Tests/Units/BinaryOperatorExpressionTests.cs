namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Linq;
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
        public void ModuloNormalizesNegativeRemainders()
        {
            Script script = new();

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
        public void ArithmeticFailsWhenOperandsAreNotNumbers()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpAdd,
                "+",
                ctx => new LiteralExpression(ctx, DynValue.True),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(1))
            );

            Assert.Throws<DynamicExpressionException>(() =>
                expr.Eval(TestHelpers.CreateExecutionContext(script))
            );
        }

        [Test]
        public void ConcatThrowsOnNonStringOperands()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpConcat,
                "..",
                ctx => new LiteralExpression(ctx, DynValue.NewPrimeTable()),
                ctx => new LiteralExpression(ctx, DynValue.NewString("value"))
            );

            Assert.Throws<DynamicExpressionException>(() =>
                expr.Eval(TestHelpers.CreateExecutionContext(script))
            );
        }

        [Test]
        public void LessThanSupportsStringComparison()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.NewString("apple")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("banana"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void LessOrEqualSupportsStringComparison()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThanEqual,
                "<=",
                ctx => new LiteralExpression(ctx, DynValue.NewString("banana")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("banana"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void LessThanThrowsOnMismatchedTypes()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.True),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(5))
            );

            Assert.Throws<DynamicExpressionException>(() =>
                expr.Eval(TestHelpers.CreateExecutionContext(script))
            );
        }

        [Test]
        public void EqualityTreatsNilAndVoidCombinationAsEqual()
        {
            Script script = new();

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
        public void EqualityReturnsFalseWhenNumbersDiffer()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpEqual,
                "==",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(1)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Boolean, Is.False);
        }

        [TestCase(TokenType.OpMinusOrSub, "-", OpCode.Sub)]
        [TestCase(TokenType.OpMul, "*", OpCode.Mul)]
        [TestCase(TokenType.OpDiv, "/", OpCode.Div)]
        [TestCase(TokenType.OpMod, "%", OpCode.Mod)]
        [TestCase(TokenType.OpPwr, "^", OpCode.Power)]
        [TestCase(TokenType.OpConcat, "..", OpCode.Concat)]
        public void CompileEmitsExpectedOpCodeForArithmeticOperators(
            int tokenTypeValue,
            string tokenText,
            int expectedOpCodeValue
        )
        {
            TokenType tokenType = (TokenType)tokenTypeValue;
            OpCode expectedOpCode = (OpCode)expectedOpCodeValue;
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                tokenType,
                tokenText,
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(3)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            ByteCode byteCode = new(script);
            expr.Compile(byteCode);

            Assert.That(
                byteCode.Code.ToArray().Select(i => i.OpCode),
                Does.Contain(expectedOpCode)
            );
        }

        [Test]
        public void ParsingUnexpectedOperatorThrows()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            object chain = BinaryOperatorExpression.BeginOperatorChain();
            BinaryOperatorExpression.AddExpressionToChain(
                chain,
                new LiteralExpression(context, DynValue.NewNumber(1))
            );

            Assert.Throws<InternalErrorException>(() =>
                BinaryOperatorExpression.AddOperatorToChain(
                    chain,
                    CreateToken(TokenType.Comma, ",")
                )
            );
        }

        [Test]
        public void EqualityReturnsFalseForMismatchedTypes()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpEqual,
                "==",
                ctx => new LiteralExpression(ctx, DynValue.Nil),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Boolean, Is.False);
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
        public void StringLessComparisonUsesLexicographicalOrdering()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.NewString("alpha")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("beta"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void StringLessOrEqualTreatsEqualStringsAsTrue()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThanEqual,
                "<=",
                ctx => new LiteralExpression(ctx, DynValue.NewString("cat")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("cat"))
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
        public void EqualityReturnsTrueForSharedDynValueReference()
        {
            Script script = new Script();
            DynValue shared = DynValue.NewTable(script);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpEqual,
                "==",
                ctx => new LiteralExpression(ctx, shared),
                ctx => new LiteralExpression(ctx, shared)
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void GreaterOrEqualComparisonReturnsTrueForEqualNumbers()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpGreaterThanEqual,
                ">=",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(4)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(4))
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

        [Test]
        public void PowerOperatorIsRightAssociative()
        {
            Script script = new Script();

            Expression expr = BuildExpressionChain(
                script,
                new[] { DynValue.NewNumber(2), DynValue.NewNumber(3), DynValue.NewNumber(2) },
                new[] { (TokenType.OpPwr, "^"), (TokenType.OpPwr, "^") }
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Number, Is.EqualTo(Math.Pow(2, Math.Pow(3, 2))));
        }

        [Test]
        public void CreatePowerExpressionBuildsExponentNode()
        {
            Script script = new Script();
            ScriptLoadingContext ctx = new(script);

            Expression expr = BinaryOperatorExpression.CreatePowerExpression(
                new LiteralExpression(ctx, DynValue.NewNumber(2)),
                new LiteralExpression(ctx, DynValue.NewNumber(5)),
                ctx
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Number, Is.EqualTo(32));
        }

        [Test]
        public void MultiplicationHasHigherPrecedenceThanAddition()
        {
            Script script = new Script();

            Expression expr = BuildExpressionChain(
                script,
                new[] { DynValue.NewNumber(1), DynValue.NewNumber(2), DynValue.NewNumber(3) },
                new[] { (TokenType.OpAdd, "+"), (TokenType.OpMul, "*") }
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            Assert.That(result.Number, Is.EqualTo(7));
        }

        [Test]
        public void CompileOrEmitsShortCircuitJump()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.Or,
                "or",
                ctx => new LiteralExpression(ctx, DynValue.True),
                ctx => new LiteralExpression(ctx, DynValue.False)
            );

            ByteCode byteCode = new(script);
            expr.Compile(byteCode);

            Assert.Multiple(() =>
            {
                Assert.That(byteCode.Code[0].OpCode, Is.EqualTo(OpCode.Literal));
                Instruction jump = byteCode.Code[1];
                Assert.That(jump.OpCode, Is.EqualTo(OpCode.JtOrPop));
                Assert.That(jump.NumVal, Is.EqualTo(byteCode.Code.Count));
                Assert.That(byteCode.Code[2].OpCode, Is.EqualTo(OpCode.Literal));
            });
        }

        [Test]
        public void CompileAndEmitsShortCircuitJump()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.And,
                "and",
                ctx => new LiteralExpression(ctx, DynValue.True),
                ctx => new LiteralExpression(ctx, DynValue.False)
            );

            ByteCode byteCode = new(script);
            expr.Compile(byteCode);

            Assert.Multiple(() =>
            {
                Assert.That(byteCode.Code[0].OpCode, Is.EqualTo(OpCode.Literal));
                Instruction jump = byteCode.Code[1];
                Assert.That(jump.OpCode, Is.EqualTo(OpCode.JfOrPop));
                Assert.That(jump.NumVal, Is.EqualTo(byteCode.Code.Count));
                Assert.That(byteCode.Code[2].OpCode, Is.EqualTo(OpCode.Literal));
            });
        }

        [Test]
        public void CompileGreaterThanInvertsComparisonResult()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpGreaterThan,
                ">",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(5)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(3))
            );

            ByteCode byteCode = new(script);
            expr.Compile(byteCode);

            Assert.Multiple(() =>
            {
                Assert.That(byteCode.Code[2].OpCode, Is.EqualTo(OpCode.LessEq));
                Assert.That(byteCode.Code[3].OpCode, Is.EqualTo(OpCode.CNot));
                Assert.That(byteCode.Code[^1].OpCode, Is.EqualTo(OpCode.Not));
            });
        }

        [Test]
        public void CompileArithmeticAndConcatEmitsExpectedOpcode()
        {
            Script script = new Script();
            (TokenType TokenType, string Text, bool OperandsAreStrings, OpCode OpCode)[] cases =
            {
                (TokenType.OpAdd, "+", false, OpCode.Add),
                (TokenType.OpMinusOrSub, "-", false, OpCode.Sub),
                (TokenType.OpMul, "*", false, OpCode.Mul),
                (TokenType.OpDiv, "/", false, OpCode.Div),
                (TokenType.OpMod, "%", false, OpCode.Mod),
                (TokenType.OpPwr, "^", false, OpCode.Power),
                (TokenType.OpConcat, "..", true, OpCode.Concat),
            };

            foreach (
                (
                    TokenType tokenType,
                    string tokenText,
                    bool operandsAreStrings,
                    OpCode expectedOpCode
                ) in cases
            )
            {
                Expression expr = BuildBinaryExpression(
                    script,
                    tokenType,
                    tokenText,
                    ctx =>
                        operandsAreStrings
                            ? new LiteralExpression(ctx, DynValue.NewString("left"))
                            : new LiteralExpression(ctx, DynValue.NewNumber(6)),
                    ctx =>
                        operandsAreStrings
                            ? new LiteralExpression(ctx, DynValue.NewString("right"))
                            : new LiteralExpression(ctx, DynValue.NewNumber(3))
                );

                ByteCode byteCode = new(script);
                expr.Compile(byteCode);

                Assert.That(byteCode.Code[^1].OpCode, Is.EqualTo(expectedOpCode), tokenText);
            }
        }

        [Test]
        public void CompileComparisonOperatorsEmitExpectedOpcode()
        {
            Script script = new Script();
            (TokenType TokenType, string Text, OpCode OpCode)[] cases =
            {
                (TokenType.OpLessThan, "<", OpCode.Less),
                (TokenType.OpLessThanEqual, "<=", OpCode.LessEq),
            };

            foreach ((TokenType tokenType, string text, OpCode expectedOpCode) in cases)
            {
                Expression expr = BuildBinaryExpression(
                    script,
                    tokenType,
                    text,
                    ctx => new LiteralExpression(ctx, DynValue.NewNumber(2)),
                    ctx => new LiteralExpression(ctx, DynValue.NewNumber(1))
                );

                ByteCode byteCode = new(script);
                expr.Compile(byteCode);

                Instruction[] instructions = byteCode.Code.ToArray();
                int comparisonIndex = Array.FindLastIndex(
                    instructions,
                    i => i.OpCode == expectedOpCode
                );

                Assert.Multiple(() =>
                {
                    Assert.That(comparisonIndex, Is.GreaterThan(1), text);

                    if (expectedOpCode == OpCode.Less)
                    {
                        Assert.That(
                            instructions[comparisonIndex + 1].OpCode,
                            Is.EqualTo(OpCode.ToBool)
                        );
                    }
                    else
                    {
                        Assert.That(
                            instructions[comparisonIndex + 1].OpCode,
                            Is.EqualTo(OpCode.CNot)
                        );
                    }
                });
            }
        }

        [Test]
        public void CompileNotEqualEmitsEqualityFollowedByNot()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpNotEqual,
                "~=",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(1)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            ByteCode byteCode = new(script);
            expr.Compile(byteCode);

            Instruction[] instructions = byteCode.Code.ToArray();
            int eqIndex = Array.FindLastIndex(instructions, i => i.OpCode == OpCode.Eq);

            Assert.Multiple(() =>
            {
                Assert.That(eqIndex, Is.GreaterThan(1));
                Assert.That(instructions[eqIndex + 1].OpCode, Is.EqualTo(OpCode.ToBool));
                Assert.That(instructions[^1].OpCode, Is.EqualTo(OpCode.Not));
            });
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

        private static Expression BuildExpressionChain(
            Script script,
            DynValue[] operands,
            (TokenType Type, string Text)[] operators
        )
        {
            if (operands.Length != operators.Length + 1)
            {
                throw new ArgumentException(
                    "Operator count must be operand count - 1",
                    nameof(operators)
                );
            }

            ScriptLoadingContext ctx = new(script);
            object chain = BinaryOperatorExpression.BeginOperatorChain();

            for (int i = 0; i < operands.Length; i++)
            {
                BinaryOperatorExpression.AddExpressionToChain(
                    chain,
                    new LiteralExpression(ctx, operands[i])
                );

                if (i < operators.Length)
                {
                    (TokenType type, string text) = operators[i];
                    BinaryOperatorExpression.AddOperatorToChain(chain, CreateToken(type, text));
                }
            }

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
