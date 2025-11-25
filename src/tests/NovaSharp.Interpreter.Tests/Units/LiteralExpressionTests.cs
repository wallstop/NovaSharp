namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LiteralExpressionTests
    {
        [TestCase("42", 42)]
        [TestCase("0x10", 16)]
        [TestCase("0x1p1", 2)]
        public void LiteralExpressionParsesNumericTokens(string literal, double expectedValue)
        {
            LiteralExpression expression = ParseLiteral(literal);

            Assert.That(expression.Value.Type, Is.EqualTo(DataType.Number));
            Assert.That(expression.Value.Number, Is.EqualTo(expectedValue));
        }

        [TestCase("\"hello\"", "hello")]
        [TestCase("[[multiline]]", "multiline")]
        public void LiteralExpressionParsesStringTokens(string literal, string expectedValue)
        {
            LiteralExpression expression = ParseLiteral(literal);

            Assert.That(expression.Value.Type, Is.EqualTo(DataType.String));
            Assert.That(expression.Value.String, Is.EqualTo(expectedValue));
        }

        [TestCase("true", true)]
        [TestCase("false", false)]
        public void LiteralExpressionParsesBooleanTokens(string literal, bool expectedValue)
        {
            LiteralExpression expression = ParseLiteral(literal);

            Assert.That(expression.Value.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(expression.Value.Boolean, Is.EqualTo(expectedValue));
        }

        [Test]
        public void LiteralExpressionParsesNilToken()
        {
            LiteralExpression expression = ParseLiteral("nil");

            Assert.That(expression.Value.IsNil(), Is.True);
        }

        [Test]
        public void LiteralExpressionRejectsUnsupportedTokens()
        {
            ScriptLoadingContext context = CreateContext("identifier");
            Token token = context.Lexer.Current;

            Assert.Throws<InternalErrorException>(() => new LiteralExpression(context, token));
        }

        private static LiteralExpression ParseLiteral(string code)
        {
            ScriptLoadingContext context = CreateContext(code);
            Token token = context.Lexer.Current;
            return new LiteralExpression(context, token);
        }

        private static ScriptLoadingContext CreateContext(string code)
        {
            Script script = new();
            SourceCode source = new("units/literal", code, script.SourceCodeCount, script);
            ScriptLoadingContext context = new(script)
            {
                Source = source,
                Scope = new BuildTimeScope(),
                Lexer = new Lexer(source.SourceId, code, true),
            };
            context.Scope.PushFunction(new DummyClosureBuilder(), hasVarArgs: false);
            context.Scope.PushBlock();
            context.Lexer.Next();
            return context;
        }

        private sealed class DummyClosureBuilder : IClosureBuilder
        {
            public SymbolRef CreateUpValue(BuildTimeScope scope, SymbolRef symbol)
            {
                return symbol;
            }
        }
    }
}
