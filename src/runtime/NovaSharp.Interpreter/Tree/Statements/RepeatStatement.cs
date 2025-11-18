namespace NovaSharp.Interpreter.Tree.Statements
{
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class RepeatStatement : Statement
    {
        private readonly Expression _condition;
        private readonly Statement _block;
        private readonly RuntimeScopeBlock _stackFrame;

        private readonly SourceRef _repeat;

        private readonly SourceRef _until;

        public RepeatStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            _repeat = CheckTokenType(lcontext, TokenType.Repeat).GetSourceRef();

            lcontext.Scope.PushBlock();
            _block = new CompositeStatement(lcontext);

            Token until = CheckTokenType(lcontext, TokenType.Until);

            _condition = Expression.Expr(lcontext);

            _until = until.GetSourceRefUpTo(lcontext.Lexer.Current);

            _stackFrame = lcontext.Scope.PopBlock();
            lcontext.Source.Refs.Add(_repeat);
            lcontext.Source.Refs.Add(_until);
        }

        public override void Compile(ByteCode bc)
        {
            Loop l = new() { Scope = _stackFrame };

            bc.PushSourceRef(_repeat);

            bc.LoopTracker.loops.Push(l);

            int start = bc.GetJumpPointForNextInstruction();

            bc.Emit_Enter(_stackFrame);
            _block.Compile(bc);

            bc.PopSourceRef();
            bc.PushSourceRef(_until);
            bc.Emit_Debug("..end");

            _condition.Compile(bc);
            bc.Emit_Leave(_stackFrame);
            bc.Emit_Jump(OpCode.Jf, start);

            bc.LoopTracker.loops.Pop();

            int exitpoint = bc.GetJumpPointForNextInstruction();

            foreach (Instruction i in l.BreakJumps)
            {
                i.NumVal = exitpoint;
            }

            bc.PopSourceRef();
        }
    }
}
