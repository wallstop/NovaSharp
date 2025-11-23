namespace NovaSharp.Interpreter.Tree.Statements
{
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class BreakStatement : Statement
    {
        private readonly SourceRef _ref;

        public BreakStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            _ref = CheckTokenType(lcontext, TokenType.Break).GetSourceRef();
            lcontext.Source.Refs.Add(_ref);
        }

        public override void Compile(ByteCode bc)
        {
            using (bc.EnterSource(_ref))
            {
                if (bc.LoopTracker.Loops.Count == 0)
                {
                    throw new SyntaxErrorException(
                        Script,
                        _ref,
                        "<break> at line {0} not inside a loop",
                        _ref.FromLine
                    );
                }

                ILoop loop = bc.LoopTracker.Loops.Peek();

                if (loop.IsBoundary())
                {
                    throw new SyntaxErrorException(
                        Script,
                        _ref,
                        "<break> at line {0} not inside a loop",
                        _ref.FromLine
                    );
                }

                loop.CompileBreak(bc);
            }
        }
    }
}
