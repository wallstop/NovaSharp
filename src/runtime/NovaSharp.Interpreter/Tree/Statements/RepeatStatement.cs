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
        private readonly CompositeStatement _block;
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

            bc.LoopTracker.Loops.Push(l);

            int start = bc.GetJumpPointForNextInstruction();

            bc.EmitEnter(_stackFrame);
            _block.Compile(bc);

            bc.PopSourceRef();
            bc.PushSourceRef(_until);
            bc.EmitDebug("..end");

            _condition.Compile(bc);
            bc.EmitLeave(_stackFrame);
            bc.EmitJump(OpCode.Jf, start);

            bc.LoopTracker.Loops.Pop();

            int exitpoint = bc.GetJumpPointForNextInstruction();

            foreach (Instruction i in l.BreakJumps)
            {
                i.NumVal = exitpoint;
            }

            bc.PopSourceRef();
        }
    }
}
