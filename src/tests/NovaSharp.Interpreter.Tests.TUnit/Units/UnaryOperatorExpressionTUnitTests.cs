#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Collections.Generic;
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

    public sealed class UnaryOperatorExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task UnaryOperatorsEvaluateExpectedResults()
        {
            IReadOnlyList<(string Script, double Expected)> cases = new[]
            {
                ("return -42", -42d),
                ("return not true", 0d),
                ("return not false", 1d),
                ("return #\"abc\"", 3d),
            };

            Script script = new();

            foreach ((string chunk, double expected) in cases)
            {
                double result = script.DoString(chunk).Number;
                await Assert.That(result).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task NegationThrowsForNonNumeric()
        {
            Script script = new();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return -\"hello\"")
            );

            await Assert.That(exception.Message).Contains("perform arithmetic on a string value");
        }

        [global::TUnit.Core.Test]
        public async Task CompileRejectsUnexpectedOperator()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            Token fakeToken = CreateToken(TokenType.OpMinusOrSub, "++");
            LiteralExpression literal = new(context, DynValue.NewNumber(1));
            UnaryOperatorExpression expression = new(context, literal, fakeToken);
            ByteCode byteCode = new(script);

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                expression.Compile(byteCode)
            );

            await Assert.That(exception.Message).Contains("Unexpected unary operator");
        }

        [global::TUnit.Core.Test]
        public async Task EvalRejectsUnexpectedOperator()
        {
            Script script = new();
            ScriptLoadingContext context = new(script);
            LiteralExpression literal = new(context, DynValue.NewNumber(1));
            Token fakeToken = CreateToken(TokenType.OpMinusOrSub, "++");
            UnaryOperatorExpression expression = new(context, literal, fakeToken);
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expression.Eval(executionContext)
            );

            await Assert.That(exception.Message).Contains("Unexpected unary operator");
        }

        [global::TUnit.Core.Test]
        public async Task EvalNegatesNumberValue()
        {
            Script script = new();
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            UnaryOperatorExpression expression = CreateExpression(
                script,
                DynValue.NewNumber(5),
                "-"
            );

            DynValue result = expression.Eval(executionContext);

            await Assert.That(result.Number).IsEqualTo(-5d);
        }

        [global::TUnit.Core.Test]
        public async Task EvalReturnsBooleanNegation()
        {
            Script script = new();
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            UnaryOperatorExpression expression = CreateExpression(script, DynValue.True, "not");

            DynValue result = expression.Eval(executionContext);

            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task EvalReturnsLengthOfValue()
        {
            Script script = new();
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            UnaryOperatorExpression expression = CreateExpression(
                script,
                DynValue.NewString("Lua"),
                "#"
            );

            DynValue result = expression.Eval(executionContext);

            await Assert.That(result.Number).IsEqualTo(3d);
        }

        [global::TUnit.Core.Test]
        public async Task EvalLengthThrowsWhenOperandHasNoLength()
        {
            Script script = new();
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            UnaryOperatorExpression expression = CreateExpression(
                script,
                DynValue.NewNumber(5),
                "#"
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                expression.Eval(executionContext)
            );

            await Assert.That(exception.Message).Contains("Can't get length of type Number");
        }

        [global::TUnit.Core.Test]
        public async Task EvalThrowsWhenNegatingNonNumberLiteral()
        {
            Script script = new();
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            UnaryOperatorExpression expression = CreateExpression(
                script,
                DynValue.NewString("hello"),
                "-"
            );

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expression.Eval(executionContext)
            );

            await Assert.That(exception.Message).Contains("Attempt to perform arithmetic");
        }

        [global::TUnit.Core.Test]
        public async Task EvalBitNotReturnsOnIntegerOperand()
        {
            Script script = new();
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            UnaryOperatorExpression expression = CreateExpression(
                script,
                DynValue.NewNumber(0b1010),
                "~"
            );

            DynValue result = expression.Eval(executionContext);

            await Assert.That(result.Number).IsEqualTo(~0b1010);
        }

        [global::TUnit.Core.Test]
        public async Task EvalBitNotThrowsOnNonInteger()
        {
            Script script = new();
            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            UnaryOperatorExpression expression = CreateExpression(
                script,
                DynValue.NewNumber(3.14),
                "~"
            );

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expression.Eval(executionContext)
            );

            await Assert.That(exception.Message).Contains("bitwise operation on non-integers");
        }

        [global::TUnit.Core.Test]
        public async Task CompileEmitsExpectedOpcodeForSupportedOperators()
        {
            (string Text, TokenType Type, OpCode Expected)[] scenarios = new[]
            {
                ("not", TokenType.Not, OpCode.Not),
                ("#", TokenType.OpLen, OpCode.Len),
                ("-", TokenType.OpMinusOrSub, OpCode.Neg),
                ("~", TokenType.OpBitNotOrXor, OpCode.BitNot),
            };

            foreach ((string text, TokenType type, OpCode expected) in scenarios)
            {
                Script script = new();
                ScriptLoadingContext context = new(script);
                LiteralExpression literal = new(context, DynValue.NewNumber(1));
                UnaryOperatorExpression expression = new(context, literal, CreateToken(type, text));
                ByteCode byteCode = new(script);

                expression.Compile(byteCode);

                Instruction emitted = byteCode.Code[^1];
                await Assert.That(emitted.OpCode).IsEqualTo(expected);
            }
        }

        private static UnaryOperatorExpression CreateExpression(
            Script script,
            DynValue operand,
            string opText
        )
        {
            ScriptLoadingContext context = new(script);
            LiteralExpression literal = new(context, operand);
            Token token = opText switch
            {
                "not" => CreateToken(TokenType.Not, opText),
                "#" => CreateToken(TokenType.OpLen, opText),
                "-" => CreateToken(TokenType.OpMinusOrSub, opText),
                "~" => CreateToken(TokenType.OpBitNotOrXor, opText),
                _ => CreateToken(TokenType.OpMinusOrSub, opText),
            };

            return new UnaryOperatorExpression(context, literal, token);
        }

        private static Token CreateToken(TokenType type, string text)
        {
            return new Token(
                type,
                sourceId: 0,
                fromLine: 1,
                fromCol: 1,
                toLine: 1,
                toCol: 1,
                prevLine: 1,
                prevCol: 1
            )
            {
                Text = text,
            };
        }
    }
}
#pragma warning restore CA2007
