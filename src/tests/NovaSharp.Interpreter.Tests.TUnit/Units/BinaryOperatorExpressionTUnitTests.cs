#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tests.Units;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;
    using Expression = NovaSharp.Interpreter.Tree.Expression;

    public sealed class BinaryOperatorExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task OrShortCircuitsWhenFirstOperandIsTruthy()
        {
            Script script = new();
            StubExpression rhsStub = null;

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

            await Assert.That(result.Boolean).IsTrue();
            await Assert.That(rhsStub).IsNotNull();
            await Assert.That(rhsStub.EvalCount).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task OrEvaluatesSecondOperandWhenFirstIsFalsey()
        {
            Script script = new();
            StubExpression rhsStub = null;

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

            await Assert.That(result.Number).IsEqualTo(42d);
            await Assert.That(rhsStub).IsNotNull();
            await Assert.That(rhsStub.EvalCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task AndShortCircuitsWhenFirstOperandIsFalsey()
        {
            Script script = new();
            StubExpression rhsStub = null;

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

            await Assert.That(result.IsNil()).IsTrue();
            await Assert.That(rhsStub).IsNotNull();
            await Assert.That(rhsStub.EvalCount).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task AndEvaluatesSecondOperandWhenFirstIsTruthy()
        {
            Script script = new();
            StubExpression rhsStub = null;

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

            await Assert.That(result.Number).IsEqualTo(1337d);
            await Assert.That(rhsStub).IsNotNull();
            await Assert.That(rhsStub.EvalCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task ModuloNormalizesNegativeRemainders()
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

            await Assert.That(result.Number).IsEqualTo(1d);
        }

        [global::TUnit.Core.Test]
        public async Task FloorDivisionUsesFlooredQuotient()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpFloorDiv,
                "//",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(-5)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(-3d);
        }

        [global::TUnit.Core.Test]
        public async Task BitwiseAndReturnsIntegerResult()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpBitAnd,
                "&",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(0xF0)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(0x0F))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(0d);
        }

        [global::TUnit.Core.Test]
        public async Task AdditionHasHigherPrecedenceThanShiftOperators()
        {
            Script script = new();
            DynValue[] operands = new DynValue[]
            {
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(1),
            };
            (TokenType Type, string Text)[] operators = new (TokenType, string)[]
            {
                (TokenType.OpAdd, "+"),
                (TokenType.OpShiftLeft, "<<"),
            };

            Expression expr = BuildExpressionChain(script, operands, operators);

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(6d);
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticFailsWhenOperandsAreNotNumbers()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpAdd,
                "+",
                ctx => new LiteralExpression(ctx, DynValue.True),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(1))
            );

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expr.Eval(TestHelpers.CreateExecutionContext(script))
            );

            await Assert.That(exception.Message).Contains("arithmetic");
        }

        [global::TUnit.Core.Test]
        public async Task ConcatThrowsOnNonStringOperands()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpConcat,
                "..",
                ctx => new LiteralExpression(ctx, DynValue.NewPrimeTable()),
                ctx => new LiteralExpression(ctx, DynValue.NewString("value"))
            );

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expr.Eval(TestHelpers.CreateExecutionContext(script))
            );

            await Assert.That(exception.Message).Contains("concatenation");
        }

        [global::TUnit.Core.Test]
        public async Task LessThanSupportsStringComparison()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task LessOrEqualSupportsStringComparison()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task LessThanThrowsOnMismatchedTypes()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.True),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(5))
            );

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expr.Eval(TestHelpers.CreateExecutionContext(script))
            );

            await Assert.That(exception.Message).Contains("compare");
        }

        [global::TUnit.Core.Test]
        public async Task EqualityTreatsNilAndVoidCombinationAsEqual()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task EqualityReturnsFalseWhenNumbersDiffer()
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

            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments((int)TokenType.OpMinusOrSub, "-", (int)OpCode.Sub)]
        [global::TUnit.Core.Arguments((int)TokenType.OpMul, "*", (int)OpCode.Mul)]
        [global::TUnit.Core.Arguments((int)TokenType.OpDiv, "/", (int)OpCode.Div)]
        [global::TUnit.Core.Arguments((int)TokenType.OpMod, "%", (int)OpCode.Mod)]
        [global::TUnit.Core.Arguments((int)TokenType.OpPwr, "^", (int)OpCode.Power)]
        [global::TUnit.Core.Arguments((int)TokenType.OpConcat, "..", (int)OpCode.Concat)]
        public async Task CompileEmitsExpectedOpCodeForArithmeticOperators(
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

            bool contains = byteCode.Code.Any(i => i.OpCode == expectedOpCode);
            await Assert.That(contains).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ParsingUnexpectedOperatorThrows()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            object chain = BinaryOperatorExpression.BeginOperatorChain();
            BinaryOperatorExpression.AddExpressionToChain(
                chain,
                new LiteralExpression(context, DynValue.NewNumber(1))
            );

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                BinaryOperatorExpression.AddOperatorToChain(
                    chain,
                    CreateToken(TokenType.Comma, ",")
                )
            );

            await Assert.That(exception.Message).Contains("Unexpected");
        }

        [global::TUnit.Core.Test]
        public async Task EqualityReturnsFalseForMismatchedTypes()
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

            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticOperationsReturnExpectedResult()
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

            await Assert.That(result.Number).IsEqualTo(10d);
        }

        [global::TUnit.Core.Test]
        public async Task ModuloProducesPositiveResult()
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

            await Assert.That(result.Number).IsEqualTo(1d);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments((int)TokenType.OpAdd, "+", 4d, 6d, 10d)]
        [global::TUnit.Core.Arguments((int)TokenType.OpMinusOrSub, "-", 9d, 3d, 6d)]
        [global::TUnit.Core.Arguments((int)TokenType.OpMul, "*", 7d, 8d, 56d)]
        [global::TUnit.Core.Arguments((int)TokenType.OpDiv, "/", 9d, 3d, 3d)]
        [global::TUnit.Core.Arguments((int)TokenType.OpPwr, "^", 2d, 3d, 8d)]
        public async Task ArithmeticOperatorsProduceExpectedNumbers(
            int tokenTypeValue,
            string tokenText,
            double left,
            double right,
            double expected
        )
        {
            TokenType tokenType = (TokenType)tokenTypeValue;

            Script script = new();
            Expression expr = BuildBinaryExpression(
                script,
                tokenType,
                tokenText,
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(left)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(right))
            );

            double result = expr.Eval(TestHelpers.CreateExecutionContext(script)).Number;

            await Assert.That(result).IsEqualTo(expected);
        }

        [global::TUnit.Core.Test]
        public async Task ArithmeticThrowsOnNonNumericOperands()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpMul,
                "*",
                ctx => new LiteralExpression(ctx, DynValue.NewString("a")),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expr.Eval(TestHelpers.CreateExecutionContext(script))
            );

            await Assert.That(exception.Message).Contains("arithmetic");
        }

        [global::TUnit.Core.Test]
        public async Task ConcatenationCombinesStrings()
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

            await Assert.That(result.String).IsEqualTo("LuaSharp");
        }

        [global::TUnit.Core.Test]
        public async Task ConcatenationThrowsForNonStringOperands()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpConcat,
                "..",
                ctx => new LiteralExpression(ctx, DynValue.NewString("Lua")),
                ctx => new LiteralExpression(ctx, DynValue.NewTable(ctx.Script))
            );

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expr.Eval(TestHelpers.CreateExecutionContext(script))
            );

            await Assert.That(exception.Message).Contains("concatenation");
        }

        [global::TUnit.Core.Test]
        public async Task ComparisonEvaluatesNumericOrdering()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ComparisonSupportsGreaterThan()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task StringLessComparisonUsesLexicographicalOrdering()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task StringLessOrEqualTreatsEqualStringsAsTrue()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task EqualityTreatsNilAndVoidAsEqual()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task EqualityReturnsTrueForSharedDynValueReference()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GreaterOrEqualComparisonReturnsTrueForEqualNumbers()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ComparisonThrowsForMismatchedTypes()
        {
            Script script = new Script();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(1)),
                ctx => new LiteralExpression(ctx, DynValue.NewString("x"))
            );

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expr.Eval(TestHelpers.CreateExecutionContext(script))
            );

            await Assert.That(exception.Message).Contains("compare");
        }

        [global::TUnit.Core.Test]
        public async Task LessComparisonSupportsStrings()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.NewString("alpha")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("beta"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GreaterComparisonUsesInvertedLessOrEqualResult()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpGreaterThan,
                ">",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(5)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(3))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task LessOrEqualThrowsForMismatchedTypes()
        {
            Script script = new();

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThanEqual,
                "<=",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(1)),
                ctx => new LiteralExpression(ctx, DynValue.NewString("x"))
            );

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expr.Eval(TestHelpers.CreateExecutionContext(script))
            );

            await Assert.That(exception.Message).Contains("compare");
        }

        [global::TUnit.Core.Test]
        public async Task EqualityHandlesNilAndVoidEquivalence()
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

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task PowerOperatorIsRightAssociative()
        {
            Script script = new Script();

            Expression expr = BuildExpressionChain(
                script,
                new[] { DynValue.NewNumber(2), DynValue.NewNumber(3), DynValue.NewNumber(2) },
                new[] { (TokenType.OpPwr, "^"), (TokenType.OpPwr, "^") }
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(Math.Pow(2, Math.Pow(3, 2)));
        }

        [global::TUnit.Core.Test]
        public async Task CreatePowerExpressionBuildsExponentNode()
        {
            Script script = new Script();
            ScriptLoadingContext ctx = new(script);

            Expression expr = BinaryOperatorExpression.CreatePowerExpression(
                new LiteralExpression(ctx, DynValue.NewNumber(2)),
                new LiteralExpression(ctx, DynValue.NewNumber(5)),
                ctx
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(32d);
        }

        [global::TUnit.Core.Test]
        public async Task MultiplicationHasHigherPrecedenceThanAddition()
        {
            Script script = new Script();

            Expression expr = BuildExpressionChain(
                script,
                new[] { DynValue.NewNumber(1), DynValue.NewNumber(2), DynValue.NewNumber(3) },
                new[] { (TokenType.OpAdd, "+"), (TokenType.OpMul, "*") }
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(7d);
        }

        [global::TUnit.Core.Test]
        public async Task CompileOrEmitsShortCircuitJump()
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

            Instruction[] instructions = byteCode.Code.ToArray();
            Instruction jump = instructions[1];

            await Assert.That(instructions[0].OpCode).IsEqualTo(OpCode.Literal);
            await Assert.That(jump.OpCode).IsEqualTo(OpCode.JtOrPop);
            await Assert.That(jump.NumVal).IsEqualTo(instructions.Length);
            await Assert.That(instructions[2].OpCode).IsEqualTo(OpCode.Literal);
        }

        [global::TUnit.Core.Test]
        public async Task CompileAndEmitsShortCircuitJump()
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

            Instruction[] instructions = byteCode.Code.ToArray();
            Instruction jump = instructions[1];

            await Assert.That(instructions[0].OpCode).IsEqualTo(OpCode.Literal);
            await Assert.That(jump.OpCode).IsEqualTo(OpCode.JfOrPop);
            await Assert.That(jump.NumVal).IsEqualTo(instructions.Length);
            await Assert.That(instructions[2].OpCode).IsEqualTo(OpCode.Literal);
        }

        [global::TUnit.Core.Test]
        public async Task CompileGreaterThanInvertsComparisonResult()
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

            Instruction[] instructions = byteCode.Code.ToArray();

            await Assert.That(instructions[2].OpCode).IsEqualTo(OpCode.LessEq);
            await Assert.That(instructions[3].OpCode).IsEqualTo(OpCode.CNot);
            await Assert.That(instructions[^1].OpCode).IsEqualTo(OpCode.Not);
        }

        [global::TUnit.Core.Test]
        public async Task CompileArithmeticAndConcatEmitsExpectedOpcode()
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

                OpCode actual = byteCode.Code[^1].OpCode;
                await Assert.That(actual).IsEqualTo(expectedOpCode);
            }
        }

        [global::TUnit.Core.Test]
        public async Task CompileComparisonOperatorsEmitExpectedOpcode()
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

                await Assert.That(comparisonIndex).IsGreaterThan(1);

                OpCode followup = instructions[comparisonIndex + 1].OpCode;
                if (expectedOpCode == OpCode.Less)
                {
                    await Assert.That(followup).IsEqualTo(OpCode.ToBool);
                }
                else
                {
                    await Assert.That(followup).IsEqualTo(OpCode.CNot);
                }
            }
        }

        [global::TUnit.Core.Test]
        public async Task CompileNotEqualEmitsEqualityFollowedByNot()
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

            await Assert.That(eqIndex).IsGreaterThan(1);
            await Assert.That(instructions[eqIndex + 1].OpCode).IsEqualTo(OpCode.ToBool);
            await Assert.That(instructions[^1].OpCode).IsEqualTo(OpCode.Not);
        }

        [global::TUnit.Core.Test]
        public async Task CommitOperatorChainThrowsWhenReductionLeavesMultipleNodes()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            object chain = BinaryOperatorExpression.BeginOperatorChain();
            BinaryOperatorExpression.AddExpressionToChain(
                chain,
                new LiteralExpression(context, DynValue.NewNumber(1))
            );
            BinaryOperatorExpression.AddExpressionToChain(
                chain,
                new LiteralExpression(context, DynValue.NewNumber(2))
            );

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                BinaryOperatorExpression.CommitOperatorChain(chain, context)
            );

            await Assert.That(exception.Message).Contains("Expression reduction didn't work! - 1");
        }

        [global::TUnit.Core.Test]
        public async Task CommitOperatorChainThrowsWhenExpressionMissing()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            object chain = BinaryOperatorExpression.BeginOperatorChain();
            BinaryOperatorExpression.AddExpressionToChain(
                chain,
                new LiteralExpression(context, DynValue.NewNumber(42))
            );
            BinaryOperatorExpression.RemoveFirstExpressionForTests(chain);

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                BinaryOperatorExpression.CommitOperatorChain(chain, context)
            );

            await Assert.That(exception.Message).Contains("Expression reduction didn't work! - 2");
        }

        [global::TUnit.Core.Test]
        public async Task CompileThrowsWhenOperatorMappingMissing()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            BinaryOperatorExpression expression = (BinaryOperatorExpression)
                BinaryOperatorExpression.CreatePowerExpression(
                    new LiteralExpression(context, DynValue.NewNumber(1)),
                    new LiteralExpression(context, DynValue.NewNumber(2)),
                    context
                );
            expression.SetOperatorForTests(BinaryOperatorExpression.Operator.NotAnOperator);

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                expression.Compile(new ByteCode(script))
            );

            await Assert.That(exception.Message).Contains("Unsupported operator");
        }

        [global::TUnit.Core.Test]
        public async Task EvalThrowsWhenComparisonOperatorCombinationUnsupported()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            BinaryOperatorExpression expression = (BinaryOperatorExpression)
                BinaryOperatorExpression.CreatePowerExpression(
                    new LiteralExpression(context, DynValue.NewNumber(3)),
                    new LiteralExpression(context, DynValue.NewNumber(4)),
                    context
                );
            BinaryOperatorExpression.Operator combined =
                BinaryOperatorExpression.Operator.Equal
                | BinaryOperatorExpression.Operator.StrConcat;
            expression.SetOperatorForTests(combined);

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expression.Eval(TestHelpers.CreateExecutionContext(script))
            );

            await Assert.That(exception.Message).Contains("Unsupported operator");
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
#pragma warning restore CA2007
