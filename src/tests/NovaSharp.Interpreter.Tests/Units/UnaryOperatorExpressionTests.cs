namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;

    [TestFixture]
    public sealed class UnaryOperatorExpressionTests
    {
        [TestCase("return -42", -42)]
        [TestCase("return not true", 0)]
        [TestCase("return not false", 1)]
        [TestCase("return #\"abc\"", 3)]
        public void UnaryOperatorsEvaluateExpectedResults(string script, double expected)
        {
            Script engine = new Script();
            double result = engine.DoString(script).Number;

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void NegationThrowsForNonNumeric()
        {
            Script engine = new Script();

            Assert.That(
                () => engine.DoString("return -\"hello\""),
                Throws
                    .TypeOf<NovaSharp.Interpreter.Errors.ScriptRuntimeException>()
                    .With.Message.Contains("attempt to perform arithmetic on a string value")
            );
        }

        [Test]
        public void CompileRejectsUnexpectedOperator()
        {
            Script script = new Script();
            Execution.ScriptLoadingContext ctx = new(script);
            Token fakeToken = new Token(TokenType.OpMinusOrSub, 0, 0, 0, 0, 0, 0, 0)
            {
                Text = "++",
            };
            LiteralExpression literal = new(ctx, DynValue.NewNumber(1));

            UnaryOperatorExpression expression = new(ctx, literal, fakeToken);

            Assert.That(
                () => expression.Compile(new Execution.VM.ByteCode(script)),
                Throws
                    .TypeOf<NovaSharp.Interpreter.Errors.InternalErrorException>()
                    .With.Message.Contains("Unexpected unary operator")
            );
        }

        [Test]
        public void EvalRejectsUnexpectedOperator()
        {
            Script script = new Script();
            Execution.ScriptLoadingContext ctx = new(script);
            LiteralExpression literal = new(ctx, DynValue.NewNumber(1));
            Token unexpectedToken = new Token(TokenType.OpMinusOrSub, 0, 0, 0, 0, 0, 0, 0)
            {
                Text = "++",
            };
            UnaryOperatorExpression expression = new(ctx, literal, unexpectedToken);

            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            Assert.That(
                () => expression.Eval(executionContext),
                Throws
                    .TypeOf<NovaSharp.Interpreter.Errors.DynamicExpressionException>()
                    .With.Message.Contains("Unexpected unary operator")
            );
        }

        [Test]
        public void EvalNegatesNumberValue()
        {
            Script script = new Script();
            Execution.ScriptLoadingContext ctx = new(script);
            LiteralExpression literal = new(ctx, DynValue.NewNumber(5));
            Token minusToken = new Token(TokenType.OpMinusOrSub, 0, 0, 0, 0, 0, 0, 0)
            {
                Text = "-",
            };
            UnaryOperatorExpression expression = new(ctx, literal, minusToken);

            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            DynValue result = expression.Eval(executionContext);

            Assert.That(result.Number, Is.EqualTo(-5));
        }

        [Test]
        public void EvalReturnsBooleanNegation()
        {
            Script script = new Script();
            Execution.ScriptLoadingContext ctx = new(script);
            LiteralExpression literal = new(ctx, DynValue.True);
            Token notToken = new Token(TokenType.Not, 0, 0, 0, 0, 0, 0, 0) { Text = "not" };
            UnaryOperatorExpression expression = new(ctx, literal, notToken);

            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            DynValue result = expression.Eval(executionContext);

            Assert.That(result.Boolean, Is.False);
        }

        [Test]
        public void EvalReturnsLengthOfValue()
        {
            Script script = new Script();
            Execution.ScriptLoadingContext ctx = new(script);
            LiteralExpression literal = new(ctx, DynValue.NewString("Lua"));
            Token lenToken = new Token(TokenType.OpLen, 0, 0, 0, 0, 0, 0, 0) { Text = "#" };
            UnaryOperatorExpression expression = new(ctx, literal, lenToken);

            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            DynValue result = expression.Eval(executionContext);

            Assert.That(result.Number, Is.EqualTo(3));
        }

        [Test]
        public void EvalLengthThrowsWhenOperandHasNoLength()
        {
            Script script = new Script();
            Execution.ScriptLoadingContext ctx = new(script);
            LiteralExpression literal = new(ctx, DynValue.NewNumber(5));
            Token lenToken = new Token(TokenType.OpLen, 0, 0, 0, 0, 0, 0, 0) { Text = "#" };
            UnaryOperatorExpression expression = new(ctx, literal, lenToken);

            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            Assert.That(
                () => expression.Eval(executionContext),
                Throws
                    .TypeOf<NovaSharp.Interpreter.Errors.ScriptRuntimeException>()
                    .With.Message.Contains("Can't get length of type Number")
            );
        }

        [Test]
        public void EvalThrowsWhenNegatingNonNumberLiteral()
        {
            Script script = new Script();
            Execution.ScriptLoadingContext ctx = new(script);
            LiteralExpression literal = new(ctx, DynValue.NewString("hello"));
            Token minusToken = new Token(TokenType.OpMinusOrSub, 0, 0, 0, 0, 0, 0, 0)
            {
                Text = "-",
            };
            UnaryOperatorExpression expression = new(ctx, literal, minusToken);

            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            Assert.That(
                () => expression.Eval(executionContext),
                Throws
                    .TypeOf<NovaSharp.Interpreter.Errors.DynamicExpressionException>()
                    .With.Message.Contains("Attempt to perform arithmetic")
            );
        }

        [Test]
        public void EvalBitNotReturnsOnIntegerOperand()
        {
            Script script = new Script();
            Execution.ScriptLoadingContext ctx = new(script);
            LiteralExpression literal = new(ctx, DynValue.NewNumber(0b1010));
            Token bitNotToken = new Token(TokenType.OpBitNotOrXor, 0, 0, 0, 0, 0, 0, 0)
            {
                Text = "~",
            };
            UnaryOperatorExpression expression = new(ctx, literal, bitNotToken);

            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);
            DynValue result = expression.Eval(executionContext);

            Assert.That(result.Number, Is.EqualTo(~0b1010));
        }

        [Test]
        public void EvalBitNotThrowsOnNonInteger()
        {
            Script script = new Script();
            Execution.ScriptLoadingContext ctx = new(script);
            LiteralExpression literal = new(ctx, DynValue.NewNumber(3.14));
            Token bitNotToken = new Token(TokenType.OpBitNotOrXor, 0, 0, 0, 0, 0, 0, 0)
            {
                Text = "~",
            };
            UnaryOperatorExpression expression = new(ctx, literal, bitNotToken);

            ScriptExecutionContext executionContext = TestHelpers.CreateExecutionContext(script);

            Assert.That(
                () => expression.Eval(executionContext),
                Throws
                    .TypeOf<NovaSharp.Interpreter.Errors.DynamicExpressionException>()
                    .With.Message.Contains("bitwise operation on non-integers")
            );
        }

        [Test]
        public void CompileEmitsExpectedOpcodeForSupportedOperators()
        {
            (string Text, TokenType TokenType, OpCode Expected)[] scenarios =
            {
                ("not", TokenType.Not, OpCode.Not),
                ("#", TokenType.OpLen, OpCode.Len),
                ("-", TokenType.OpMinusOrSub, OpCode.Neg),
                ("~", TokenType.OpBitNotOrXor, OpCode.BitNot),
            };

            foreach ((string text, TokenType tokenType, OpCode expected) in scenarios)
            {
                Script script = new Script();
                Execution.ScriptLoadingContext ctx = new(script);
                LiteralExpression literal = new(ctx, DynValue.NewNumber(1));
                Token token = CreateToken(tokenType, text);

                UnaryOperatorExpression expression = new(ctx, literal, token);
                ByteCode byteCode = new(script);

                expression.Compile(byteCode);

                Instruction emitted = byteCode.Code[^1];
                Assert.That(emitted.OpCode, Is.EqualTo(expected));
            }
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
