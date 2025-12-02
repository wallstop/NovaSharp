namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;

    public sealed class LiteralExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task LiteralExpressionParsesNumericTokens()
        {
            await AssertLiteral("42", DataType.Number, 42d).ConfigureAwait(false);
            await AssertLiteral("0x10", DataType.Number, 16d).ConfigureAwait(false);
            await AssertLiteral("0x1p1", DataType.Number, 2d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LiteralExpressionParsesStringTokens()
        {
            await AssertLiteral("\"hello\"", DataType.String, "hello").ConfigureAwait(false);
            await AssertLiteral("[[multiline]]", DataType.String, "multiline")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LiteralExpressionParsesBooleanTokens()
        {
            await AssertLiteral("true", DataType.Boolean, true).ConfigureAwait(false);
            await AssertLiteral("false", DataType.Boolean, false).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LiteralExpressionParsesNilToken()
        {
            LiteralExpression expression = ParseLiteral("nil");
            await Assert.That(expression.Value.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LiteralExpressionRejectsUnsupportedTokens()
        {
            ScriptLoadingContext context = CreateContext("identifier");
            Token token = context.Lexer.Current;

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
            {
                _ = new LiteralExpression(context, token);
            })!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        private static async Task AssertLiteral(string code, DataType expectedType, object expected)
        {
            LiteralExpression expression = ParseLiteral(code);
            await Assert.That(expression.Value.Type).IsEqualTo(expectedType).ConfigureAwait(false);
            switch (expectedType)
            {
                case DataType.Number:
                    await Assert
                        .That(expression.Value.Number)
                        .IsEqualTo((double)expected)
                        .ConfigureAwait(false);
                    break;
                case DataType.String:
                    await Assert
                        .That(expression.Value.String)
                        .IsEqualTo((string)expected)
                        .ConfigureAwait(false);
                    break;
                case DataType.Boolean:
                    await Assert
                        .That(expression.Value.Boolean)
                        .IsEqualTo((bool)expected)
                        .ConfigureAwait(false);
                    break;
            }
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
