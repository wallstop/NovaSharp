namespace WallstopStudios.NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using Debugging;
    using Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a Lua <c>if</c>/<c>elseif</c>/<c>else</c> block.
    /// </summary>
    internal class IfStatement : Statement
    {
        private class IfBlock
        {
            /// <summary>
            /// Gets or sets the expression that controls whether the block executes.
            /// </summary>
            public Expression Condition { get; set; }

            /// <summary>
            /// Gets or sets the statements executed when the condition evaluates to true.
            /// </summary>
            public CompositeStatement Body { get; set; }

            /// <summary>
            /// Gets or sets the runtime scope captured for the block.
            /// </summary>
            public RuntimeScopeBlock StackFrame { get; set; }

            /// <summary>
            /// Gets or sets the source reference covering the block (for debugging).
            /// </summary>
            public SourceRef Source { get; set; }
        }

        private readonly List<IfBlock> _ifs = new();
        private readonly IfBlock _else;
        private readonly SourceRef _end;

        /// <summary>
        /// Parses the conditional chain (zero or more <c>elseif</c> blocks plus an optional <c>else</c> block).
        /// </summary>
        public IfStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            while (
                lcontext.Lexer.Current.type != TokenType.Else
                && lcontext.Lexer.Current.type != TokenType.End
            )
            {
                _ifs.Add(CreateIfBlock(lcontext));
            }

            if (lcontext.Lexer.Current.type == TokenType.Else)
            {
                _else = CreateElseBlock(lcontext);
            }

            _end = CheckTokenType(lcontext, TokenType.End).GetSourceRef();
            lcontext.Source.Refs.Add(_end);
        }

        private static IfBlock CreateIfBlock(ScriptLoadingContext lcontext)
        {
            Token type = CheckTokenType(lcontext, TokenType.If, TokenType.ElseIf);

            lcontext.Scope.PushBlock();

            IfBlock ifblock = new()
            {
                Condition = Expression.Expr(lcontext),
                Source = type.GetSourceRef(CheckTokenType(lcontext, TokenType.Then)),
                Body = new CompositeStatement(lcontext),
                StackFrame = lcontext.Scope.PopBlock(),
            };

            lcontext.Source.Refs.Add(ifblock.Source);

            return ifblock;
        }

        private static IfBlock CreateElseBlock(ScriptLoadingContext lcontext)
        {
            Token type = CheckTokenType(lcontext, TokenType.Else);

            lcontext.Scope.PushBlock();

            IfBlock ifblock = new()
            {
                Body = new CompositeStatement(lcontext),
                StackFrame = lcontext.Scope.PopBlock(),
                Source = type.GetSourceRef(),
            };
            lcontext.Source.Refs.Add(ifblock.Source);
            return ifblock;
        }

        /// <summary>
        /// Emits the conditional control flow, patching all jump addresses once the block layout is known.
        /// </summary>
        public override void Compile(ByteCode bc)
        {
            List<Instruction> endJumps = new();

            Instruction lastIfJmp = null;

            foreach (IfBlock ifblock in _ifs)
            {
                using (bc.EnterSource(ifblock.Source))
                {
                    if (lastIfJmp != null)
                    {
                        lastIfJmp.NumVal = bc.GetJumpPointForNextInstruction();
                    }

                    ifblock.Condition.Compile(bc);
                    lastIfJmp = bc.EmitJump(OpCode.Jf, -1);
                    bc.EmitEnter(ifblock.StackFrame);
                    ifblock.Body.Compile(bc);
                }

                using (bc.EnterSource(_end))
                {
                    bc.EmitLeave(ifblock.StackFrame);
                }

                endJumps.Add(bc.EmitJump(OpCode.Jump, -1));
            }

            lastIfJmp.NumVal = bc.GetJumpPointForNextInstruction();

            if (_else != null)
            {
                using (bc.EnterSource(_else.Source))
                {
                    bc.EmitEnter(_else.StackFrame);
                    _else.Body.Compile(bc);
                }

                {
                    bc.EmitLeave(_else.StackFrame);
                }
            }

            foreach (Instruction endjmp in endJumps)
            {
                endjmp.NumVal = bc.GetJumpPointForNextInstruction();
            }
        }
    }
}
