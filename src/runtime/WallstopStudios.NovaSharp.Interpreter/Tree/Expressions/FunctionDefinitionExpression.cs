namespace WallstopStudios.NovaSharp.Interpreter.Tree.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Debugging;
    using Execution.Scopes;
    using Statements;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a parsed Lua function literal (including lambdas) and knows how to emit its body.
    /// </summary>
    /// <remarks>
    /// The expression captures lexical scope information (parameters, varargs, upvalues, and the
    /// enclosing environment) so compilation can recreate closures with the same behaviour Lua
    /// expects when evaluating <c>function</c> statements.
    /// </remarks>
    internal class FunctionDefinitionExpression : Expression, IClosureBuilder
    {
        private readonly SymbolRef[] _paramNames;
        private readonly Statement _statement;
        private readonly RuntimeScopeFrame _stackFrame;
        private readonly List<SymbolRef> _closure = new();
        private bool _hasVarArgs;
        private Instruction _closureInstruction;

        private readonly bool _usesGlobalEnv;
        private readonly SymbolRef _env;

        private readonly SourceRef _begin;

        private SourceRef _end;

        public FunctionDefinitionExpression(ScriptLoadingContext lcontext, bool usesGlobalEnv)
            : this(lcontext, false, usesGlobalEnv, false) { }

        public FunctionDefinitionExpression(
            ScriptLoadingContext lcontext,
            bool pushSelfParam,
            bool isLambda
        )
            : this(lcontext, pushSelfParam, false, isLambda) { }

        private FunctionDefinitionExpression(
            ScriptLoadingContext lcontext,
            bool pushSelfParam,
            bool usesGlobalEnv,
            bool isLambda
        )
            : base(lcontext)
        {
            if (_usesGlobalEnv = usesGlobalEnv)
            {
                CheckTokenType(lcontext, TokenType.Function);
            }

            // here lexer should be at the '(' or at the '|'
            Token openRound = CheckTokenType(
                lcontext,
                isLambda ? TokenType.Pipe : TokenType.BrkOpenRound
            );

            List<string> paramnames = BuildParamList(lcontext, pushSelfParam, openRound, isLambda);
            // here lexer is at first token of body

            _begin = openRound.GetSourceRefUpTo(lcontext.Lexer.Current);

            // create scope
            lcontext.Scope.PushFunction(this, _hasVarArgs);

            if (_usesGlobalEnv)
            {
                _env = lcontext.Scope.DefineLocal(WellKnownSymbols.ENV);
            }
            else
            {
                lcontext.Scope.ForceEnvUpValue();
            }

            _paramNames = DefineArguments(paramnames, lcontext);

            if (isLambda)
            {
                _statement = CreateLambdaBody(lcontext);
            }
            else
            {
                _statement = CreateBody(lcontext);
            }

            _stackFrame = lcontext.Scope.PopFunction();

            lcontext.Source.Refs.Add(_begin);
            lcontext.Source.Refs.Add(_end);
        }

        private static Statement CreateLambdaBody(ScriptLoadingContext lcontext)
        {
            Token start = lcontext.Lexer.Current;
            Expression e = Expr(lcontext);
            Token end = lcontext.Lexer.Current;
            SourceRef sref = start.GetSourceRefUpTo(end);
            Statement s = new ReturnStatement(lcontext, e, sref);
            return s;
        }

        private Statement CreateBody(ScriptLoadingContext lcontext)
        {
            Statement s = new CompositeStatement(lcontext);

            if (lcontext.Lexer.Current.type != TokenType.End)
            {
                throw new SyntaxErrorException(
                    lcontext.Lexer.Current,
                    "'end' expected near '{0}'",
                    lcontext.Lexer.Current.text
                )
                {
                    IsPrematureStreamTermination = (lcontext.Lexer.Current.type == TokenType.Eof),
                };
            }

            _end = lcontext.Lexer.Current.GetSourceRef();

            lcontext.Lexer.Next();
            return s;
        }

        private List<string> BuildParamList(
            ScriptLoadingContext lcontext,
            bool pushSelfParam,
            Token openBracketToken,
            bool isLambda
        )
        {
            TokenType closeToken = isLambda ? TokenType.Pipe : TokenType.BrkCloseRound;

            List<string> paramnames = new();

            // method decls with ':' must push an implicit 'self' param
            if (pushSelfParam)
            {
                paramnames.Add("self");
            }

            while (lcontext.Lexer.Current.type != closeToken)
            {
                Token t = lcontext.Lexer.Current;

                if (t.type == TokenType.Name)
                {
                    paramnames.Add(t.text);
                }
                else if (t.type == TokenType.VarArgs)
                {
                    _hasVarArgs = true;
                    paramnames.Add(WellKnownSymbols.VARARGS);
                }
                else
                {
                    UnexpectedTokenType(t);
                }

                lcontext.Lexer.Next();

                t = lcontext.Lexer.Current;

                if (t.type == TokenType.Comma)
                {
                    lcontext.Lexer.Next();
                }
                else
                {
                    CheckMatch(lcontext, openBracketToken, closeToken, isLambda ? "|" : ")");
                    break;
                }
            }

            if (lcontext.Lexer.Current.type == closeToken)
            {
                lcontext.Lexer.Next();
            }

            return paramnames;
        }

        private static SymbolRef[] DefineArguments(
            List<string> paramnames,
            ScriptLoadingContext lcontext
        )
        {
            HashSet<string> names = new();

            SymbolRef[] ret = new SymbolRef[paramnames.Count];

            for (int i = paramnames.Count - 1; i >= 0; i--)
            {
                if (!names.Add(paramnames[i]))
                {
                    paramnames[i] = paramnames[i] + "@" + i.ToString(CultureInfo.InvariantCulture);
                }

                ret[i] = lcontext.Scope.DefineLocal(paramnames[i]);
            }

            return ret;
        }

        /// <summary>
        /// Creates or reuses a closure entry for the supplied symbol and returns an upvalue reference.
        /// </summary>
        /// <param name="scope">Scope that owns the symbol being captured.</param>
        /// <param name="symbol">Symbol that must remain accessible inside the compiled function.</param>
        /// <returns>An upvalue pointing to the captured symbol.</returns>
        public SymbolRef CreateUpValue(BuildTimeScope scope, SymbolRef symbol)
        {
            for (int i = 0; i < _closure.Count; i++)
            {
                if (_closure[i].NameValue == symbol.NameValue)
                {
                    return SymbolRef.UpValue(symbol.NameValue, i);
                }
            }

            _closure.Add(symbol);

            if (_closureInstruction != null)
            {
                _closureInstruction.SymbolList = _closure.ToArray();
            }

            return SymbolRef.UpValue(symbol.NameValue, _closure.Count - 1);
        }

        /// <summary>
        /// Dynamic expressions cannot introduce new functions, so evaluation always fails.
        /// </summary>
        /// <param name="context">Execution context requesting the function value.</param>
        /// <returns>Nothing; this method always throws.</returns>
        /// <exception cref="DynamicExpressionException">Always thrown to signal invalid usage.</exception>
        public override DynValue Eval(ScriptExecutionContext context)
        {
            throw new DynamicExpressionException(
                "Dynamic Expressions cannot define new functions."
            );
        }

        /// <summary>
        /// Emits the bytecode for the function body and returns the instruction pointer of the entry.
        /// </summary>
        /// <param name="bc">The bytecode builder currently emitting instructions.</param>
        /// <param name="friendlyName">
        /// Optional descriptive name used for debugger metadata; falls back to the source location.
        /// </param>
        /// <returns>The instruction pointer pointing at the first opcode of the function body.</returns>
        public int CompileBody(ByteCode bc, string friendlyName)
        {
            string funcName = friendlyName ?? ("<" + _begin.FormatLocation(bc.Script, true) + ">");

            bc.PushSourceRef(_begin);

            Instruction i = bc.EmitJump(OpCode.Jump, -1);

            Instruction meta = bc.EmitMeta(funcName, OpCodeMetadataType.FunctionEntrypoint);
            int metaip = bc.GetJumpPointForLastInstruction();

            bc.EmitBeginFn(_stackFrame);

            bc.LoopTracker.Loops.Push(new LoopBoundary());

            int entryPoint = bc.GetJumpPointForLastInstruction();

            if (_usesGlobalEnv)
            {
                bc.EmitLoad(SymbolRef.UpValue(WellKnownSymbols.ENV, 0));
                bc.EmitStore(_env, 0, 0);
                bc.EmitPop();
            }

            if (_paramNames.Length > 0)
            {
                bc.EmitArgs(_paramNames);
            }

            _statement.Compile(bc);

            bc.PopSourceRef();
            bc.PushSourceRef(_end);

            bc.EmitRet(0);

            bc.LoopTracker.Loops.Pop();

            i.NumVal = bc.GetJumpPointForNextInstruction();
            meta.NumVal = bc.GetJumpPointForLastInstruction() - metaip;

            bc.PopSourceRef();

            return entryPoint;
        }

        /// <summary>
        /// Emits the closure wrapper for this function and allows the caller to insert extra opcodes.
        /// </summary>
        /// <param name="bc">The bytecode builder currently emitting instructions.</param>
        /// <param name="afterDecl">
        /// Callback that emits additional instructions immediately after the closure declaration and
        /// returns how many opcodes it injected so jump offsets can be adjusted.
        /// </param>
        /// <param name="friendlyName">Optional name reported in debugger metadata.</param>
        /// <returns>The instruction pointer pointing at the start of the compiled function body.</returns>
        public int Compile(ByteCode bc, Func<int> afterDecl, string friendlyName)
        {
            using (bc.EnterSource(_begin))
            {
                SymbolRef[] symbs = _closure
                //.Select((s, idx) => s.CloneLocalAndSetFrame(_ClosureFrames[idx]))
                .ToArray();

                _closureInstruction = bc.EmitClosure(symbs, bc.GetJumpPointForNextInstruction());
                int ops = afterDecl();

                _closureInstruction.NumVal += 2 + ops;
            }

            return CompileBody(bc, friendlyName);
        }

        /// <summary>
        /// Compiles the function definition and leaves the resulting closure on the stack.
        /// </summary>
        /// <param name="bc">The bytecode builder currently emitting instructions.</param>
        public override void Compile(ByteCode bc)
        {
            Compile(bc, () => 0, null);
        }
    }
}
