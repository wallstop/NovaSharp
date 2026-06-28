namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree.Expressions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Expressions;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;
    using Expression = NovaSharp.Interpreter.Tree.Expression;

    public sealed class BinaryOperatorExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OrShortCircuitsWhenFirstOperandIsTruthy(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(rhsStub).IsNotNull().ConfigureAwait(false);
            await Assert.That(rhsStub.EvalCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OrEvaluatesSecondOperandWhenFirstIsFalsey(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(rhsStub).IsNotNull().ConfigureAwait(false);
            await Assert.That(rhsStub.EvalCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AndShortCircuitsWhenFirstOperandIsFalsey(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(rhsStub).IsNotNull().ConfigureAwait(false);
            await Assert.That(rhsStub.EvalCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AndEvaluatesSecondOperandWhenFirstIsTruthy(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
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

            await Assert.That(result.Number).IsEqualTo(1337d).ConfigureAwait(false);
            await Assert.That(rhsStub).IsNotNull().ConfigureAwait(false);
            await Assert.That(rhsStub.EvalCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ModuloNormalizesNegativeRemainders(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpMod,
                "%",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(-3)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FloorDivisionUsesFlooredQuotient(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpFloorDiv,
                "//",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(-5)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(-3d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task BitwiseAndReturnsIntegerResult(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpBitAnd,
                "&",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(0xF0)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(0x0F))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AdditionHasHigherPrecedenceThanShiftOperators(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
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

            await Assert.That(result.Number).IsEqualTo(6d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ArithmeticFailsWhenOperandsAreNotNumbers(LuaCompatibilityVersion version)
        {
            Script script = new(version);

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

            await Assert.That(exception.Message).Contains("arithmetic").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ConcatThrowsOnNonStringOperands(LuaCompatibilityVersion version)
        {
            Script script = new(version);

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

            await Assert.That(exception.Message).Contains("concatenation").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LessThanSupportsStringComparison(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.NewString("apple")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("banana"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LessOrEqualSupportsStringComparison(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThanEqual,
                "<=",
                ctx => new LiteralExpression(ctx, DynValue.NewString("banana")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("banana"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LessThanPreservesIntegerPrecisionAtBoundaries(
            LuaCompatibilityVersion version
        )
        {
            // Test that large integers are compared correctly in DynamicExpression.Eval().
            // Before the fix, this would incorrectly compare as equal due to double precision loss.
            Script script = new(version);
            const long maxIntegerMinusOne = long.MaxValue - 1;
            const long maxInteger = long.MaxValue;

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(
                    ctx,
                    DynValue.NewNumber(LuaNumber.FromInteger(maxIntegerMinusOne))
                ),
                ctx => new LiteralExpression(
                    ctx,
                    DynValue.NewNumber(LuaNumber.FromInteger(maxInteger))
                )
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            // 9223372036854775806 < 9223372036854775807 should be true
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LessOrEqualPreservesIntegerPrecisionAtBoundaries(
            LuaCompatibilityVersion version
        )
        {
            // Test that large integers are compared correctly in DynamicExpression.Eval().
            Script script = new(version);
            const long maxInteger = long.MaxValue;

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThanEqual,
                "<=",
                ctx => new LiteralExpression(
                    ctx,
                    DynValue.NewNumber(LuaNumber.FromInteger(maxInteger))
                ),
                ctx => new LiteralExpression(
                    ctx,
                    DynValue.NewNumber(LuaNumber.FromInteger(maxInteger))
                )
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            // maxinteger <= maxinteger should be true
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AdditionPreservesIntegerSubtype(LuaCompatibilityVersion version)
        {
            // Test that integer arithmetic in DynamicExpression.Eval() preserves the integer subtype.
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpAdd,
                "+",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(LuaNumber.FromInteger(10))),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(LuaNumber.FromInteger(20)))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(result.LuaNumber.AsInteger).IsEqualTo(30L).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SubtractionPreservesIntegerSubtype(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpMinusOrSub,
                "-",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(LuaNumber.FromInteger(50))),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(LuaNumber.FromInteger(20)))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(result.LuaNumber.AsInteger).IsEqualTo(30L).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MultiplicationPreservesIntegerSubtype(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpMul,
                "*",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(LuaNumber.FromInteger(6))),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(LuaNumber.FromInteger(7)))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(result.LuaNumber.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FloorDivisionPreservesIntegerSubtype(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpFloorDiv,
                "//",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(LuaNumber.FromInteger(17))),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(LuaNumber.FromInteger(5)))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            // 17 // 5 = 3 (integer)
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(result.LuaNumber.AsInteger).IsEqualTo(3L).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LessThanThrowsOnMismatchedTypes(LuaCompatibilityVersion version)
        {
            Script script = new(version);

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

            await Assert.That(exception.Message).Contains("compare").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task EqualityTreatsNilAndVoidCombinationAsEqual(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpEqual,
                "==",
                ctx => new LiteralExpression(ctx, DynValue.Nil),
                ctx => new LiteralExpression(ctx, DynValue.Void)
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task EqualityReturnsFalseWhenNumbersDiffer(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpEqual,
                "==",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(1)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpMinusOrSub,
            "-",
            (int)OpCode.Sub
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpMul,
            "*",
            (int)OpCode.Mul
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpDiv,
            "/",
            (int)OpCode.Div
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpMod,
            "%",
            (int)OpCode.Mod
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpPwr,
            "^",
            (int)OpCode.Power
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpConcat,
            "..",
            (int)OpCode.Concat
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpMinusOrSub,
            "-",
            (int)OpCode.Sub
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpMul,
            "*",
            (int)OpCode.Mul
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpDiv,
            "/",
            (int)OpCode.Div
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpMod,
            "%",
            (int)OpCode.Mod
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpPwr,
            "^",
            (int)OpCode.Power
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpConcat,
            "..",
            (int)OpCode.Concat
        )]
        public async Task CompileEmitsExpectedOpCodeForArithmeticOperators(
            LuaCompatibilityVersion version,
            int tokenTypeValue,
            string tokenText,
            int expectedOpCodeValue
        )
        {
            TokenType tokenType = (TokenType)tokenTypeValue;
            OpCode expectedOpCode = (OpCode)expectedOpCodeValue;

            Script script = new(version);

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
            await Assert.That(contains).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ParsingUnexpectedOperatorThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(exception.Message).Contains("Unexpected").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task EqualityReturnsFalseForMismatchedTypes(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpEqual,
                "==",
                ctx => new LiteralExpression(ctx, DynValue.Nil),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
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

            await Assert.That(result.Number).IsEqualTo(10d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ModuloProducesPositiveResult(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpMod,
                "%",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(-3)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpAdd,
            "+",
            4d,
            6d,
            10d
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpMinusOrSub,
            "-",
            9d,
            3d,
            6d
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpMul,
            "*",
            7d,
            8d,
            56d
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpDiv,
            "/",
            9d,
            3d,
            3d
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua51,
            (int)TokenType.OpPwr,
            "^",
            2d,
            3d,
            8d
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpAdd,
            "+",
            4d,
            6d,
            10d
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpMinusOrSub,
            "-",
            9d,
            3d,
            6d
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpMul,
            "*",
            7d,
            8d,
            56d
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpDiv,
            "/",
            9d,
            3d,
            3d
        )]
        [global::TUnit.Core.Arguments(
            LuaCompatibilityVersion.Lua54,
            (int)TokenType.OpPwr,
            "^",
            2d,
            3d,
            8d
        )]
        public async Task ArithmeticOperatorsProduceExpectedNumbers(
            LuaCompatibilityVersion version,
            int tokenTypeValue,
            string tokenText,
            double left,
            double right,
            double expected
        )
        {
            TokenType tokenType = (TokenType)tokenTypeValue;

            Script script = new(version);
            Expression expr = BuildBinaryExpression(
                script,
                tokenType,
                tokenText,
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(left)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(right))
            );

            double result = expr.Eval(TestHelpers.CreateExecutionContext(script)).Number;

            await Assert.That(result).IsEqualTo(expected).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ArithmeticThrowsOnNonNumericOperands(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

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

            await Assert.That(exception.Message).Contains("arithmetic").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ConcatenationCombinesStrings(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpConcat,
                "..",
                ctx => new LiteralExpression(ctx, DynValue.NewString("Lua")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("Sharp"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.String).IsEqualTo("LuaSharp").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ConcatenationThrowsForNonStringOperands(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

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

            await Assert.That(exception.Message).Contains("concatenation").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ComparisonEvaluatesNumericOrdering(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(3)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(7))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ComparisonSupportsGreaterThan(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpGreaterThan,
                ">",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(9)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(2))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task StringLessComparisonUsesLexicographicalOrdering(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.NewString("alpha")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("beta"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task StringLessOrEqualTreatsEqualStringsAsTrue(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThanEqual,
                "<=",
                ctx => new LiteralExpression(ctx, DynValue.NewString("cat")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("cat"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task EqualityTreatsNilAndVoidAsEqual(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpEqual,
                "==",
                ctx => new LiteralExpression(ctx, DynValue.Nil),
                ctx => new LiteralExpression(ctx, DynValue.Void)
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task EqualityReturnsTrueForSharedDynValueReference(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version);
            DynValue shared = DynValue.NewTable(script);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpEqual,
                "==",
                ctx => new LiteralExpression(ctx, shared),
                ctx => new LiteralExpression(ctx, shared)
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task GreaterOrEqualComparisonReturnsTrueForEqualNumbers(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpGreaterThanEqual,
                ">=",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(4)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(4))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ComparisonThrowsForMismatchedTypes(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

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

            await Assert.That(exception.Message).Contains("compare").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LessComparisonSupportsStrings(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpLessThan,
                "<",
                ctx => new LiteralExpression(ctx, DynValue.NewString("alpha")),
                ctx => new LiteralExpression(ctx, DynValue.NewString("beta"))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GreaterComparisonUsesInvertedLessOrEqualResult(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);

            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpGreaterThan,
                ">",
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(5)),
                ctx => new LiteralExpression(ctx, DynValue.NewNumber(3))
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LessOrEqualThrowsForMismatchedTypes(LuaCompatibilityVersion version)
        {
            Script script = new(version);

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

            await Assert.That(exception.Message).Contains("compare").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task EqualityHandlesNilAndVoidEquivalence(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Expression expr = BuildBinaryExpression(
                script,
                TokenType.OpEqual,
                "==",
                ctx => new LiteralExpression(ctx, DynValue.Nil),
                ctx => new LiteralExpression(ctx, DynValue.Void)
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PowerOperatorIsRightAssociative(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

            Expression expr = BuildExpressionChain(
                script,
                new[] { DynValue.NewNumber(2), DynValue.NewNumber(3), DynValue.NewNumber(2) },
                new[] { (TokenType.OpPwr, "^"), (TokenType.OpPwr, "^") }
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert
                .That(result.Number)
                .IsEqualTo(Math.Pow(2, Math.Pow(3, 2)))
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CreatePowerExpressionBuildsExponentNode(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);
            ScriptLoadingContext ctx = new(script);

            Expression expr = BinaryOperatorExpression.CreatePowerExpression(
                new LiteralExpression(ctx, DynValue.NewNumber(2)),
                new LiteralExpression(ctx, DynValue.NewNumber(5)),
                ctx
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(32d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MultiplicationHasHigherPrecedenceThanAddition(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version);

            Expression expr = BuildExpressionChain(
                script,
                new[] { DynValue.NewNumber(1), DynValue.NewNumber(2), DynValue.NewNumber(3) },
                new[] { (TokenType.OpAdd, "+"), (TokenType.OpMul, "*") }
            );

            DynValue result = expr.Eval(TestHelpers.CreateExecutionContext(script));

            await Assert.That(result.Number).IsEqualTo(7d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileOrEmitsShortCircuitJump(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

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

            await Assert
                .That(instructions[0].OpCode)
                .IsEqualTo(OpCode.Literal)
                .ConfigureAwait(false);
            await Assert.That(jump.OpCode).IsEqualTo(OpCode.JtOrPop).ConfigureAwait(false);
            await Assert.That(jump.NumVal).IsEqualTo(instructions.Length).ConfigureAwait(false);
            await Assert
                .That(instructions[2].OpCode)
                .IsEqualTo(OpCode.Literal)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileAndEmitsShortCircuitJump(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

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

            await Assert
                .That(instructions[0].OpCode)
                .IsEqualTo(OpCode.Literal)
                .ConfigureAwait(false);
            await Assert.That(jump.OpCode).IsEqualTo(OpCode.JfOrPop).ConfigureAwait(false);
            await Assert.That(jump.NumVal).IsEqualTo(instructions.Length).ConfigureAwait(false);
            await Assert
                .That(instructions[2].OpCode)
                .IsEqualTo(OpCode.Literal)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileGreaterThanSwapsOperandsAndUsesLess(
            LuaCompatibilityVersion version
        )
        {
            // Per Lua spec: a > b is compiled as b < a (swap operands, use Less).
            // This preserves correct NaN handling: nan > nan = false.
            Script script = new Script(version);

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

            // Operands are swapped: second operand (3) is pushed first, then first operand (5)
            await Assert
                .That(instructions[0].OpCode)
                .IsEqualTo(OpCode.Literal)
                .ConfigureAwait(false);
            await Assert
                .That(instructions[1].OpCode)
                .IsEqualTo(OpCode.Literal)
                .ConfigureAwait(false);
            // Uses Less opcode (not LessEq)
            await Assert.That(instructions[2].OpCode).IsEqualTo(OpCode.Less).ConfigureAwait(false);
            // ToBool follows Less per EmitOperator
            await Assert
                .That(instructions[3].OpCode)
                .IsEqualTo(OpCode.ToBool)
                .ConfigureAwait(false);
            // No Not opcode at the end
            await Assert.That(instructions.Length).IsEqualTo(4).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileArithmeticAndConcatEmitsExpectedOpcode(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version);
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
                await Assert.That(actual).IsEqualTo(expectedOpCode).ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileComparisonOperatorsEmitExpectedOpcode(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version);
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

                await Assert.That(comparisonIndex).IsGreaterThan(1).ConfigureAwait(false);

                OpCode followup = instructions[comparisonIndex + 1].OpCode;
                if (expectedOpCode == OpCode.Less)
                {
                    await Assert.That(followup).IsEqualTo(OpCode.ToBool).ConfigureAwait(false);
                }
                else
                {
                    await Assert.That(followup).IsEqualTo(OpCode.CNot).ConfigureAwait(false);
                }
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CompileNotEqualEmitsEqualityFollowedByNot(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

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

            await Assert.That(eqIndex).IsGreaterThan(1).ConfigureAwait(false);
            await Assert
                .That(instructions[eqIndex + 1].OpCode)
                .IsEqualTo(OpCode.ToBool)
                .ConfigureAwait(false);
            await Assert.That(instructions[^1].OpCode).IsEqualTo(OpCode.Not).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CommitOperatorChainThrowsWhenReductionLeavesMultipleNodes(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
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

            await Assert
                .That(exception.Message)
                .Contains("Expression reduction didn't work! - 1")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CommitOperatorChainThrowsWhenExpressionMissing(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
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

            await Assert
                .That(exception.Message)
                .Contains("Expression reduction didn't work! - 2")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CompileThrowsWhenOperatorMappingMissing(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert
                .That(exception.Message)
                .Contains("Unsupported operator")
                .ConfigureAwait(false);
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

            await Assert
                .That(exception.Message)
                .Contains("Unsupported operator")
                .ConfigureAwait(false);
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
            return new Token(type, 0, 0, 0, 0, 0, 0, 0, text);
        }

        private sealed class StubExpression : Expression
        {
            private readonly Func<ScriptExecutionContext, DynValue> _evaluate;

            public StubExpression(
                ScriptLoadingContext context,
                Func<ScriptExecutionContext, DynValue> evaluate
            )
                : base(context)
            {
                _evaluate = evaluate;
            }

            public int EvalCount { get; private set; }

            public override void Compile(ByteCode bc)
            {
                // No-op for coverage purposes; tests exercise Eval directly.
            }

            public override DynValue Eval(ScriptExecutionContext context)
            {
                EvalCount++;
                return _evaluate(context);
            }
        }
    }
}
