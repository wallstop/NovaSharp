namespace WallstopStudios.NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a sequence of statements (block) delimited by Lua keywords such as <c>do ... end</c>.
    /// </summary>
    internal class CompositeStatement : Statement
    {
        private readonly List<Statement> _statements = new();

        /// <summary>
        /// Parses statements until an end-of-block token is encountered, respecting Lua's "return must be last" rule.
        /// </summary>
        public CompositeStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            while (true)
            {
                Token t = lcontext.Lexer.Current;
                if (t.IsEndOfBlock())
                {
                    break;
                }

                // Per Lua 5.4 §3.5: snapshot var count before statement parsing
                // so we know which locals were added by this statement.
                lcontext.Scope.BeforeStatement();

                Statement s = CreateStatement(lcontext, out bool forceLast);
                _statements.Add(s);

                // Per Lua 5.4 §3.5: track non-void statements for label scope computation.
                // Void statements are labels and empty statements (semicolons).
                if (!s.IsVoidStatement)
                {
                    lcontext.Scope.MarkNonVoidStatement();
                }

                if (forceLast)
                {
                    break;
                }
            }

            // eat away all superfluous ';'s
            while (lcontext.Lexer.Current.type == TokenType.SemiColon)
            {
                lcontext.Lexer.Next();
            }
        }

        /// <summary>
        /// Compiles each contained statement in order.
        /// </summary>
        public override void Compile(Execution.VM.ByteCode bc)
        {
            if (_statements != null)
            {
                foreach (Statement s in _statements)
                {
                    s.Compile(bc);
                }
            }
        }
    }
}
