namespace NovaSharp.Interpreter.Tree.Statements
{
    using Debugging;
    using Execution;
    using Execution.VM;
    using Expressions;

    internal class ForLoopStatement : Statement
    {
        //for' NAME '=' exp ',' exp (',' exp)? 'do' block 'end'
        private readonly RuntimeScopeBlock _stackFrame;
        private readonly Statement _innerBlock;
        private readonly SymbolRef _varName;

        private readonly Expression _start;

        private readonly Expression _end;

        private readonly Expression _step;

        private readonly SourceRef _refFor;

        private readonly SourceRef _refEnd;

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

        public override void Compile(ByteCode bc)
        {
            bc.PushSourceRef(_refFor);

            Loop l = new() { scope = _stackFrame };

            bc.LoopTracker.loops.Push(l);

            _end.Compile(bc);
            bc.Emit_ToNum(3);
            _step.Compile(bc);
            bc.Emit_ToNum(2);
            _start.Compile(bc);
            bc.Emit_ToNum(1);

            int start = bc.GetJumpPointForNextInstruction();
            Instruction jumpend = bc.Emit_Jump(OpCode.JFor, -1);
            bc.Emit_Enter(_stackFrame);
            //bc.Emit_SymStorN(_VarName);

            bc.Emit_Store(_varName, 0, 0);

            _innerBlock.Compile(bc);

            bc.PopSourceRef();
            bc.PushSourceRef(_refEnd);

            bc.Emit_Debug("..end");
            bc.Emit_Leave(_stackFrame);
            bc.Emit_Incr(1);
            bc.Emit_Jump(OpCode.Jump, start);

            bc.LoopTracker.loops.Pop();

            int exitpoint = bc.GetJumpPointForNextInstruction();

            foreach (Instruction i in l.breakJumps)
            {
                i.NumVal = exitpoint;
            }

            jumpend.NumVal = exitpoint;
            bc.Emit_Pop(3);

            bc.PopSourceRef();
        }
    }
}
