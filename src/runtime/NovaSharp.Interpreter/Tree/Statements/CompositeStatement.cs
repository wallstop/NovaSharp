namespace NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class CompositeStatement : Statement
    {
        private readonly List<Statement> _statements = new();

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

            // eat away all superfluos ';'s
            while (lcontext.Lexer.Current.type == TokenType.SemiColon)
            {
                lcontext.Lexer.Next();
            }
        }

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
