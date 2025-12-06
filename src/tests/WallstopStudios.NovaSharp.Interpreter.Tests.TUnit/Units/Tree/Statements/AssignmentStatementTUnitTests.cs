namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree.Statements
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Statements;

    public sealed class AssignmentStatementTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task LocalAssignmentAcceptsConstAndCloseAttributes()
        {
            ScriptLoadingContext context = CreateContext("local resource <const><close> = 1");
            Token localToken = context.Lexer.Current;
            context.Lexer.Next();

            AssignmentStatement statement = new(context, localToken);
            await Assert.That(statement).IsNotNull().ConfigureAwait(false);

            SymbolRef symbol = context.Scope.Find("resource");
            await Assert
                .That(symbol.Attributes)
                .IsEqualTo(SymbolRefAttributes.Const | SymbolRefAttributes.ToBeClosed)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LocalAssignmentRejectsDuplicateAttributes()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                ParseLocalAssignment("local duplicate <const><const> = 1")
            )!;

            await Assert
                .That(exception.Message)
                .Contains("duplicate attribute 'const'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LocalAssignmentRejectsUnknownAttributes()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                ParseLocalAssignment("local value <fast> = 1")
            )!;

            await Assert
                .That(exception.Message)
                .Contains("unknown attribute 'fast'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstAttributeRequiresLua54Compatibility()
        {
            Script script = new();
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua53;

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                ParseLocalAssignment("local legacy <const> = 1", script)
            )!;

            await Assert
                .That(exception.Message)
                .Contains("Lua 5.4+ compatibility")
                .ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("§3.3.7").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CloseAttributeRequiresLua54Compatibility()
        {
            Script script = new();
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua53;

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                ParseLocalAssignment("local guard <close> = newcloser()", script)
            )!;

            await Assert
                .That(exception.Message)
                .Contains("Lua 5.4+ compatibility")
                .ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("§3.3.8").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AssignmentRequiresWritableVariables()
        {
            Script script = new();

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("1 = 2")
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
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
