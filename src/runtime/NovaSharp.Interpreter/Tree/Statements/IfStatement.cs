namespace NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class IfStatement : Statement
    {
        private class IfBlock
        {
            public Expression exp;
            public Statement block;
            public RuntimeScopeBlock stackFrame;
            public SourceRef source;
        }

        private readonly List<IfBlock> _ifs = new();
        private readonly IfBlock _else;
        private readonly SourceRef _end;

        public IfStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            while (
                lcontext.Lexer.Current.Type != TokenType.Else
                && lcontext.Lexer.Current.Type != TokenType.End
            )
            {
                _ifs.Add(CreateIfBlock(lcontext));
            }

            if (lcontext.Lexer.Current.Type == TokenType.Else)
            {
                _else = CreateElseBlock(lcontext);
            }

            _end = CheckTokenType(lcontext, TokenType.End).GetSourceRef();
            lcontext.Source.Refs.Add(_end);
        }

        private IfBlock CreateIfBlock(ScriptLoadingContext lcontext)
        {
            Token type = CheckTokenType(lcontext, TokenType.If, TokenType.ElseIf);

            lcontext.Scope.PushBlock();

            IfBlock ifblock = new()
            {
                exp = Expression.Expr(lcontext),
                source = type.GetSourceRef(CheckTokenType(lcontext, TokenType.Then)),
                block = new CompositeStatement(lcontext),
                stackFrame = lcontext.Scope.PopBlock(),
            };

            lcontext.Source.Refs.Add(ifblock.source);

            return ifblock;
        }

        private IfBlock CreateElseBlock(ScriptLoadingContext lcontext)
        {
            Token type = CheckTokenType(lcontext, TokenType.Else);

            lcontext.Scope.PushBlock();

            IfBlock ifblock = new()
            {
                block = new CompositeStatement(lcontext),
                stackFrame = lcontext.Scope.PopBlock(),
                source = type.GetSourceRef(),
            };
            lcontext.Source.Refs.Add(ifblock.source);
            return ifblock;
        }

        public override void Compile(ByteCode bc)
        {
            List<Instruction> endJumps = new();

            Instruction lastIfJmp = null;

            foreach (IfBlock ifblock in _ifs)
            {
                using (bc.EnterSource(ifblock.source))
                {
                    if (lastIfJmp != null)
                    {
                        lastIfJmp.NumVal = bc.GetJumpPointForNextInstruction();
                    }

                    ifblock.exp.Compile(bc);
                    lastIfJmp = bc.Emit_Jump(OpCode.Jf, -1);
                    bc.Emit_Enter(ifblock.stackFrame);
                    ifblock.block.Compile(bc);
                }

                using (bc.EnterSource(_end))
                {
                    bc.Emit_Leave(ifblock.stackFrame);
                }

                endJumps.Add(bc.Emit_Jump(OpCode.Jump, -1));
            }

            lastIfJmp.NumVal = bc.GetJumpPointForNextInstruction();

            if (_else != null)
            {
                using (bc.EnterSource(_else.source))
                {
                    bc.Emit_Enter(_else.stackFrame);
                    _else.block.Compile(bc);
                }

                {
                    bc.Emit_Leave(_else.stackFrame);
                }
            }

            foreach (Instruction endjmp in endJumps)
            {
                endjmp.NumVal = bc.GetJumpPointForNextInstruction();
            }
        }
    }
}
