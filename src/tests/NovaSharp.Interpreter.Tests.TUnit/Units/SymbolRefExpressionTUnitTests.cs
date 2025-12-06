namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;

    public sealed class SymbolRefExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task VarArgsAreRejectedInsideDynamicExpressions()
        {
            Script script = new();
            ScriptLoadingContext context = CreateDynamicContext(script, hasVarArgs: true);
            context.Scope.DefineLocal(WellKnownSymbols.VARARGS);

            Token token = new(TokenType.VarArgs, 0, 1, 1, 1, 3, 1, 0)
            {
                Text = WellKnownSymbols.VARARGS,
            };

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
            {
                GC.KeepAlive(new SymbolRefExpression(token, context));
            });

            await Assert
                .That(exception.Message)
                .Contains("cannot use '...' in a dynamic expression")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SymbolReferenceConstructorRejectsDynamicExpressions()
        {
            Script script = new();
            ScriptLoadingContext context = CreateDynamicContext(script, hasVarArgs: false);

            SymbolRef symbol = SymbolRef.Global("value", SymbolRef.DefaultEnv);

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
            {
                GC.KeepAlive(new SymbolRefExpression(context, symbol));
            });

            await Assert
                .That(exception.Message)
                .Contains("Unsupported symbol reference expression")
                .ConfigureAwait(false);
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
            public SymbolRef CreateUpValue(BuildTimeScope scope, SymbolRef symbol)
            {
                return symbol;
            }
        }
    }
}
