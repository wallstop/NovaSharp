namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NovaSharp.Interpreter.Tree.Statements;
    using NUnit.Framework;

    [TestFixture]
    public sealed class AssignmentStatementTests
    {
        [Test]
        public void LocalAssignmentAcceptsConstAndCloseAttributes()
        {
            ScriptLoadingContext context = CreateContext("local resource <const><close> = 1");
            Token localToken = context.Lexer.Current;
            context.Lexer.Next();

            Assert.DoesNotThrow(() => new AssignmentStatement(context, localToken));

            SymbolRef symbol = context.Scope.Find("resource");
            Assert.That(
                symbol.Attributes,
                Is.EqualTo(SymbolRefAttributes.Const | SymbolRefAttributes.ToBeClosed)
            );
        }

        [Test]
        public void LocalAssignmentRejectsDuplicateAttributes()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                ParseLocalAssignment("local duplicate <const><const> = 1")
            )!;

            Assert.That(exception.Message, Does.Contain("duplicate attribute 'const'"));
        }

        [Test]
        public void LocalAssignmentRejectsUnknownAttributes()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                ParseLocalAssignment("local value <fast> = 1")
            )!;

            Assert.That(exception.Message, Does.Contain("unknown attribute 'fast'"));
        }

        [Test]
        public void AssignmentRequiresWritableVariables()
        {
            Script script = new();

            Assert.Throws<SyntaxErrorException>(() => script.DoString("1 = 2"));
        }

        private static AssignmentStatement ParseLocalAssignment(string code)
        {
            ScriptLoadingContext context = CreateContext(code);
            Token localToken = context.Lexer.Current;
            context.Lexer.Next();
            return new AssignmentStatement(context, localToken);
        }

        private static ScriptLoadingContext CreateContext(string code)
        {
            Script script = new();
            SourceCode source = new("units/assignment", code, script.SourceCodeCount, script);
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
            public SymbolRef CreateUpvalue(BuildTimeScope scope, SymbolRef symbol)
            {
                return symbol;
            }
        }
    }
}
