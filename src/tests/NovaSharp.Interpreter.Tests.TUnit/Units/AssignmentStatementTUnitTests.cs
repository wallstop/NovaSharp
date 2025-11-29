#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NovaSharp.Interpreter.Tree.Statements;

    public sealed class AssignmentStatementTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task LocalAssignmentAcceptsConstAndCloseAttributes()
        {
            ScriptLoadingContext context = CreateContext("local resource <const><close> = 1");
            Token localToken = context.Lexer.Current;
            context.Lexer.Next();

            AssignmentStatement statement = new(context, localToken);
            await Assert.That(statement).IsNotNull();

            SymbolRef symbol = context.Scope.Find("resource");
            await Assert.That(symbol.Attributes)
                .IsEqualTo(SymbolRefAttributes.Const | SymbolRefAttributes.ToBeClosed);
        }

        [global::TUnit.Core.Test]
        public async Task LocalAssignmentRejectsDuplicateAttributes()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                ParseLocalAssignment("local duplicate <const><const> = 1")
            )!;

            await Assert.That(exception.Message).Contains("duplicate attribute 'const'");
        }

        [global::TUnit.Core.Test]
        public async Task LocalAssignmentRejectsUnknownAttributes()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                ParseLocalAssignment("local value <fast> = 1")
            )!;

            await Assert.That(exception.Message).Contains("unknown attribute 'fast'");
        }

        [global::TUnit.Core.Test]
        public async Task ConstAttributeRequiresLua54Compatibility()
        {
            Script script = new();
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua53;

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                ParseLocalAssignment("local legacy <const> = 1", script)
            )!;

            await Assert.That(exception.Message).Contains("Lua 5.4+ compatibility");
            await Assert.That(exception.Message).Contains("§3.3.7");
        }

        [global::TUnit.Core.Test]
        public async Task CloseAttributeRequiresLua54Compatibility()
        {
            Script script = new();
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua53;

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                ParseLocalAssignment("local guard <close> = newcloser()", script)
            )!;

            await Assert.That(exception.Message).Contains("Lua 5.4+ compatibility");
            await Assert.That(exception.Message).Contains("§3.3.8");
        }

        [global::TUnit.Core.Test]
        public async Task AssignmentRequiresWritableVariables()
        {
            Script script = new();

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("1 = 2")
            );

            await Assert.That(exception).IsNotNull();
        }

        private static AssignmentStatement ParseLocalAssignment(string code, Script script = null)
        {
            ScriptLoadingContext context = CreateContext(code, script);
            Token localToken = context.Lexer.Current;
            context.Lexer.Next();
            return new AssignmentStatement(context, localToken);
        }

        private static ScriptLoadingContext CreateContext(string code, Script script = null)
        {
            Script effectiveScript = script ?? new Script();
            SourceCode source = new(
                "units/assignment",
                code,
                effectiveScript.SourceCodeCount,
                effectiveScript
            );
            ScriptLoadingContext context = new(effectiveScript)
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
#pragma warning restore CA2007
