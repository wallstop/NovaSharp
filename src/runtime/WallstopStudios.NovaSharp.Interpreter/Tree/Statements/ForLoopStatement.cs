namespace WallstopStudios.NovaSharp.Interpreter.Tree.Statements
{
    using global::NovaSharp;
    using Debugging;
    using Execution.Scopes;
    using Expressions;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a numeric Lua <c>for</c> loop.
    /// </summary>
    internal class ForLoopStatement : Statement
    {
        //for' NAME '=' exp ',' exp (',' exp)? 'do' block 'end'
        private readonly RuntimeScopeBlock _stackFrame;
        private readonly CompositeStatement _innerBlock;
        private readonly SymbolRef _varName;

        private readonly Expression _start;

        private readonly Expression _end;

        private readonly Expression _step;

        private readonly SourceRef _refFor;

        private readonly SourceRef _refEnd;

        /// <summary>
        /// Parses the numeric <c>for</c> loop, capturing the range expressions and loop body.
        /// </summary>
        /// <param name="lcontext">Parser context providing the lexer/token stream.</param>
        /// <param name="nameToken">Token containing the loop variable name.</param>
        /// <param name="forToken">Token for the `for` keyword (used for diagnostics).</param>
        public ForLoopStatement(ScriptLoadingContext lcontext, Token nameToken, Token forToken)
            : base(lcontext)
        {
            //	for Name ‘=’ exp ‘,’ exp [‘,’ exp] do block end |

            // lexer already at the '=' ! [due to dispatching vs for-each]
            CheckTokenType(lcontext, TokenType.OpAssignment);

            _start = Expression.Expr(lcontext);
            CheckTokenType(lcontext, TokenType.Comma);
            _end = Expression.Expr(lcontext);

            if (lcontext.Lexer.Current.type == TokenType.Comma)
            {
                lcontext.Lexer.Next();
                _step = Expression.Expr(lcontext);
            }
            else
            {
                _step = new LiteralExpression(lcontext, LuaValue.NewNumber(1));
            }

            lcontext.Scope.PushBlock();
            _varName = lcontext.Scope.DefineLocal(nameToken.text);
            _refFor = forToken.GetSourceRef(CheckTokenType(lcontext, TokenType.Do));
            _innerBlock = new CompositeStatement(lcontext);
            _refEnd = CheckTokenType(lcontext, TokenType.End).GetSourceRef();
            _stackFrame = lcontext.Scope.PopBlock();
            _stackFrame.ValueStackSlots = 3;

            lcontext.Source.Refs.Add(_refFor);
            lcontext.Source.Refs.Add(_refEnd);
        }

        /// <summary>
        /// Compiles the numeric loop following Lua §3.3.5 and reference Lua's instruction shape:
        /// ForPrep validates and prepares the control triple and jumps past the loop when it must
        /// not run, the body follows immediately, and the bottom JFor advances the controls and
        /// jumps back for the next iteration. A break or goto leaving the loop pops the triple.
        /// </summary>
        public override void Compile(ByteCode bc)
        {
            bc.PushSourceRef(_refFor);

            using (Loop l = new() { Scope = _stackFrame })
            {
                bc.LoopTracker.Loops.Push(l);

                _end.Compile(bc);
                _step.Compile(bc);
                _start.Compile(bc);

                Instruction jumpend = bc.EmitJump(OpCode.ForPrep, -1);

                int bodyStart = bc.GetJumpPointForNextInstruction();
                bc.EmitEnter(_stackFrame);

                bc.EmitStore(_varName, 0, 0);

                _innerBlock.Compile(bc);

                bc.PopSourceRef();
                bc.PushSourceRef(_refEnd);

                bc.EmitDebug("..end");
                bc.EmitLeave(_stackFrame);
                bc.EmitIncr(1);
                bc.EmitJump(OpCode.JFor, bodyStart);

                bc.LoopTracker.Loops.Pop();

                int exitpoint = bc.GetJumpPointForNextInstruction();

                foreach (Instruction i in l.BreakJumps)
                {
                    i.NumVal = exitpoint;
                }

                jumpend.NumVal = exitpoint;
                bc.EmitPop(3);

                bc.PopSourceRef();
            }
        }
    }
}
