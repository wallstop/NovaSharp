namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;

    [TestFixture]
    public sealed class SymbolRefExpressionTests
    {
        [Test]
        public void VarArgsAreRejectedInsideDynamicExpressions()
        {
            Script script = new();
            ScriptLoadingContext context = CreateDynamicContext(script, hasVarArgs: true);
            context.Scope.DefineLocal(WellKnownSymbols.VARARGS);

            Token token = new(TokenType.VarArgs, 0, 1, 1, 1, 3, 1, 0)
            {
                Text = WellKnownSymbols.VARARGS,
            };

            Assert.That(
                () => new SymbolRefExpression(token, context),
                Throws
                    .TypeOf<DynamicExpressionException>()
                    .With.Message.Contains("cannot use '...' in a dynamic expression")
            );
        }

        [Test]
        public void SymbolReferenceConstructorRejectsDynamicExpressions()
        {
            Script script = new();
            ScriptLoadingContext context = CreateDynamicContext(script, hasVarArgs: false);

            SymbolRef symbol = SymbolRef.Global("value", SymbolRef.DefaultEnv);

            Assert.That(
                () => new SymbolRefExpression(context, symbol),
                Throws
                    .TypeOf<DynamicExpressionException>()
                    .With.Message.Contains("Unsupported symbol reference expression")
            );
        }

        private static ScriptLoadingContext CreateDynamicContext(Script script, bool hasVarArgs)
        {
            ScriptLoadingContext context = new(script)
            {
                IsDynamicExpression = true,
                Scope = new BuildTimeScope(),
                Lexer = new Lexer(0, string.Empty, true),
            };

            context.Scope.PushFunction(new PassthroughClosureBuilder(), hasVarArgs);
            context.Scope.PushBlock();
            return context;
        }

        private sealed class PassthroughClosureBuilder : IClosureBuilder
        {
            public SymbolRef CreateUpvalue(BuildTimeScope scope, SymbolRef symbol)
            {
                return symbol;
            }
        }
    }
}
