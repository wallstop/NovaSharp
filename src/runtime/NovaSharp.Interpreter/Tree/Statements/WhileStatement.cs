namespace NovaSharp.Interpreter.Tree.Statements
{
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class WhileStatement : Statement
    {
        private readonly Expression _condition;
        private readonly Statement _block;
        private readonly RuntimeScopeBlock _stackFrame;

        private readonly SourceRef _start;

        private readonly SourceRef _end;

        public WhileStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            Token whileTk = CheckTokenType(lcontext, TokenType.While);

            _condition = Expression.Expr(lcontext);

            _start = whileTk.GetSourceRefUpTo(lcontext.Lexer.Current);

            //m_Start = BuildSourceRef(context.Start, exp.Stop);
            //m_End = BuildSourceRef(context.Stop, context.END());

            lcontext.Scope.PushBlock();
            CheckTokenType(lcontext, TokenType.Do);
            _block = new CompositeStatement(lcontext);
            _end = CheckTokenType(lcontext, TokenType.End).GetSourceRef();
            _stackFrame = lcontext.Scope.PopBlock();

            lcontext.Source.Refs.Add(_start);
            lcontext.Source.Refs.Add(_end);
        }

        public override void Compile(ByteCode bc)
        {
            Loop l = new() { Scope = _stackFrame };

            bc.LoopTracker.loops.Push(l);

            bc.PushSourceRef(_start);

            int start = bc.GetJumpPointForNextInstruction();

            _condition.Compile(bc);
            Instruction jumpend = bc.Emit_Jump(OpCode.Jf, -1);

            bc.Emit_Enter(_stackFrame);

            _block.Compile(bc);

            bc.PopSourceRef();
            bc.Emit_Debug("..end");
            bc.PushSourceRef(_end);

            bc.Emit_Leave(_stackFrame);
            bc.Emit_Jump(OpCode.Jump, start);

            bc.LoopTracker.loops.Pop();

            int exitpoint = bc.GetJumpPointForNextInstruction();

            foreach (Instruction i in l.BreakJumps)
            {
                i.NumVal = exitpoint;
            }

            jumpend.NumVal = exitpoint;

            bc.PopSourceRef();
        }
    }
}
