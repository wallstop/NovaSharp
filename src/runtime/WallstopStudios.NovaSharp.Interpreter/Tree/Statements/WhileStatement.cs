namespace WallstopStudios.NovaSharp.Interpreter.Tree.Statements
{
    using Debugging;
    using Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a Lua <c>while</c> loop.
    /// </summary>
    internal class WhileStatement : Statement
    {
        private readonly Expression _condition;
        private readonly CompositeStatement _block;
        private readonly RuntimeScopeBlock _stackFrame;

        private readonly SourceRef _start;

        private readonly SourceRef _end;

        /// <summary>
        /// Parses the loop condition and body for a <c>while ... do ... end</c> construct.
        /// </summary>
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

        /// <summary>
        /// Compiles the while loop, emitting the condition check, loop body, and back-edge jump.
        /// </summary>
        public override void Compile(ByteCode bc)
        {
            using (Loop l = new() { Scope = _stackFrame })
            {
                bc.LoopTracker.Loops.Push(l);

                bc.PushSourceRef(_start);

                int start = bc.GetJumpPointForNextInstruction();

                _condition.Compile(bc);
                Instruction jumpend = bc.EmitJump(OpCode.Jf, -1);

                bc.EmitEnter(_stackFrame);

                _block.Compile(bc);

                bc.PopSourceRef();
                bc.EmitDebug("..end");
                bc.PushSourceRef(_end);

                bc.EmitLeave(_stackFrame);
                bc.EmitJump(OpCode.Jump, start);

                bc.LoopTracker.Loops.Pop();

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
}
