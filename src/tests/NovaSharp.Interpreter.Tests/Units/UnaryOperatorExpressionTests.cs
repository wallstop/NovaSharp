namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
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
            Token fakeToken = new Token(TokenType.OpMinusOrSub, 0, 0, 0, 0, 0, 0, 0) { Text = "~" };
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
                Text = "~",
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
    }
}
