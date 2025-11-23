namespace NovaSharp.Interpreter.Tree.Statements
{
    using Debugging;
    using Execution.Scopes;
    using Expressions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

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

            if (lcontext.Lexer.Current.Type == TokenType.Comma)
            {
                lcontext.Lexer.Next();
                _step = Expression.Expr(lcontext);
            }
            else
            {
                _step = new LiteralExpression(lcontext, DynValue.NewNumber(1));
            }

            lcontext.Scope.PushBlock();
            _varName = lcontext.Scope.DefineLocal(nameToken.Text);
            _refFor = forToken.GetSourceRef(CheckTokenType(lcontext, TokenType.Do));
            _innerBlock = new CompositeStatement(lcontext);
            _refEnd = CheckTokenType(lcontext, TokenType.End).GetSourceRef();
            _stackFrame = lcontext.Scope.PopBlock();

            lcontext.Source.Refs.Add(_refFor);
            lcontext.Source.Refs.Add(_refEnd);
        }

        /// <summary>
        /// Compiles the numeric loop following Lua §3.3.5: evaluates bounds/step, emits the iteration prologue, body, and increment logic.
        /// </summary>
        public override void Compile(ByteCode bc)
        {
            bc.PushSourceRef(_refFor);

            Loop l = new() { Scope = _stackFrame };

            bc.LoopTracker.Loops.Push(l);

            _end.Compile(bc);
            bc.EmitToNum(3);
            _step.Compile(bc);
            bc.EmitToNum(2);
            _start.Compile(bc);
            bc.EmitToNum(1);

            int start = bc.GetJumpPointForNextInstruction();
            Instruction jumpend = bc.EmitJump(OpCode.JFor, -1);
            bc.EmitEnter(_stackFrame);
            //bc.Emit_SymStorN(_VarName);

            bc.EmitStore(_varName, 0, 0);

            _innerBlock.Compile(bc);

            bc.PopSourceRef();
            bc.PushSourceRef(_refEnd);

            bc.EmitDebug("..end");
            bc.EmitLeave(_stackFrame);
            bc.EmitIncr(1);
            bc.EmitJump(OpCode.Jump, start);

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
