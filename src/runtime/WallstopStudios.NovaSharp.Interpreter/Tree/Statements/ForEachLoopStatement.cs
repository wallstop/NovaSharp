namespace WallstopStudios.NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using Debugging;
    using Execution.Scopes;
    using Expressions;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a Lua generic for loop (`for name in explist do ... end`).
    /// </summary>
    internal class ForEachLoopStatement : Statement
    {
        private readonly RuntimeScopeBlock _stackFrame;
        private readonly SymbolRef[] _names;
        private readonly IVariable[] _nameExps;
        private readonly ExprListExpression _rValues;
        private readonly CompositeStatement _block;

        private readonly SourceRef _refFor;

        private readonly SourceRef _refEnd;

        /// <summary>
        /// Parses a generic for loop, capturing iterator names, expressions, and the loop body.
        /// </summary>
        public ForEachLoopStatement(
            ScriptLoadingContext lcontext,
            Token firstNameToken,
            Token forToken
        )
            : base(lcontext)
        {
            //	for namelist in explist do block end |

            List<string> names = new();
            names.Add(firstNameToken.text);

            while (lcontext.Lexer.Current.type == TokenType.Comma)
            {
                lcontext.Lexer.Next();
                Token name = CheckTokenType(lcontext, TokenType.Name);
                names.Add(name.text);
            }

            CheckTokenType(lcontext, TokenType.In);

            _rValues = new ExprListExpression(Expression.ExprList(lcontext), lcontext);

            lcontext.Scope.PushBlock();

            _names = new SymbolRef[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                _names[i] = lcontext.Scope.TryDefineLocal(names[i]);
            }

            _nameExps = new IVariable[_names.Length];
            for (int i = 0; i < _names.Length; i++)
            {
                _nameExps[i] = new SymbolRefExpression(lcontext, _names[i]);
            }

            _refFor = forToken.GetSourceRef(CheckTokenType(lcontext, TokenType.Do));

            _block = new CompositeStatement(lcontext);

            _refEnd = CheckTokenType(lcontext, TokenType.End).GetSourceRef();

            _stackFrame = lcontext.Scope.PopBlock();

            lcontext.Source.Refs.Add(_refFor);
            lcontext.Source.Refs.Add(_refEnd);
        }

        /// <summary>
        /// Compiles the `for ... in ...` construct per Lua §3.3.5, including iterator preparation, per-iteration assignment, and loop exit patching.
        /// </summary>
        public override void Compile(ByteCode bc)
        {
            //for var_1, ···, var_n in explist do block end

            bc.PushSourceRef(_refFor);

            Loop l = new() { Scope = _stackFrame };
            bc.LoopTracker.Loops.Push(l);

            // get iterator tuple
            _rValues.Compile(bc);

            // prepares iterator tuple - stack : iterator-tuple
            bc.EmitIterPrep();

            // loop start - stack : iterator-tuple
            int start = bc.GetJumpPointForNextInstruction();
            bc.EmitEnter(_stackFrame);

            // expand the tuple - stack : iterator-tuple, f, var, s
            bc.EmitExpTuple(0);

            // calls f(s, var) - stack : iterator-tuple, iteration result
            bc.EmitCall(2, "for..in");

            // perform assignment of iteration result- stack : iterator-tuple, iteration result
            for (int i = 0; i < _nameExps.Length; i++)
            {
                _nameExps[i].CompileAssignment(bc, 0, i);
            }

            // pops  - stack : iterator-tuple
            bc.EmitPop();

            // repushes the main iterator var - stack : iterator-tuple, main-iterator-var
            bc.EmitLoad(_names[0]);

            // updates the iterator tuple - stack : iterator-tuple, main-iterator-var
            bc.EmitIterUpd();

            // checks head, jumps if nil - stack : iterator-tuple, main-iterator-var
            Instruction endjump = bc.EmitJump(OpCode.JNil, -1);

            // executes the stuff - stack : iterator-tuple
            _block.Compile(bc);

            bc.PopSourceRef();
            bc.PushSourceRef(_refEnd);

            // loop back again - stack : iterator-tuple
            bc.EmitLeave(_stackFrame);
            bc.EmitJump(OpCode.Jump, start);

            bc.LoopTracker.Loops.Pop();

            int exitpointLoopExit = bc.GetJumpPointForNextInstruction();
            bc.EmitLeave(_stackFrame);

            int exitpointBreaks = bc.GetJumpPointForNextInstruction();

            bc.EmitPop();

            foreach (Instruction i in l.BreakJumps)
            {
                i.NumVal = exitpointBreaks;
            }

            endjump.NumVal = exitpointLoopExit;

            bc.PopSourceRef();
        }
    }
}
