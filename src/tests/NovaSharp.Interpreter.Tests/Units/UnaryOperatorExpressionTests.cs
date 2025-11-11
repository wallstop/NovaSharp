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
    }
}
