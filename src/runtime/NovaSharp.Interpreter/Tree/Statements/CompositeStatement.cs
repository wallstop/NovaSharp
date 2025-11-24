namespace NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

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

                Statement s = CreateStatement(lcontext, out bool forceLast);
                _statements.Add(s);

                if (forceLast)
                {
                    break;
                }
            }

            // eat away all superfluous ';'s
            while (lcontext.Lexer.Current.Type == TokenType.SemiColon)
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
