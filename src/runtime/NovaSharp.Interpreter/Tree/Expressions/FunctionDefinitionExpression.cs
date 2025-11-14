namespace NovaSharp.Interpreter.Tree.Expressions
{
    using System;
    using System.Collections.Generic;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;
    using Statements;

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
                isLambda ? TokenType.Lambda : TokenType.BrkOpenRound
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

        private Statement CreateLambdaBody(ScriptLoadingContext lcontext)
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
                    lcontext.Lexer.Current.Text
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
            TokenType closeToken = isLambda ? TokenType.Lambda : TokenType.BrkCloseRound;

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
                    paramnames.Add(t.Text);
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

        private SymbolRef[] DefineArguments(List<string> paramnames, ScriptLoadingContext lcontext)
        {
            HashSet<string> names = new();

            SymbolRef[] ret = new SymbolRef[paramnames.Count];

            for (int i = paramnames.Count - 1; i >= 0; i--)
            {
                if (!names.Add(paramnames[i]))
                {
                    paramnames[i] = paramnames[i] + "@" + i.ToString();
                }

                ret[i] = lcontext.Scope.DefineLocal(paramnames[i]);
            }

            return ret;
        }

        public SymbolRef CreateUpvalue(BuildTimeScope scope, SymbolRef symbol)
        {
            for (int i = 0; i < _closure.Count; i++)
            {
                if (_closure[i].i_Name == symbol.i_Name)
                {
                    return SymbolRef.Upvalue(symbol.i_Name, i);
                }
            }

            _closure.Add(symbol);

            if (_closureInstruction != null)
            {
                _closureInstruction.SymbolList = _closure.ToArray();
            }

            return SymbolRef.Upvalue(symbol.i_Name, _closure.Count - 1);
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            throw new DynamicExpressionException(
                "Dynamic Expressions cannot define new functions."
            );
        }

        public int CompileBody(ByteCode bc, string friendlyName)
        {
            string funcName = friendlyName ?? ("<" + _begin.FormatLocation(bc.Script, true) + ">");

            bc.PushSourceRef(_begin);

            Instruction i = bc.Emit_Jump(OpCode.Jump, -1);

            Instruction meta = bc.Emit_Meta(funcName, OpCodeMetadataType.FunctionEntrypoint);
            int metaip = bc.GetJumpPointForLastInstruction();

            bc.Emit_BeginFn(_stackFrame);

            bc.LoopTracker.loops.Push(new LoopBoundary());

            int entryPoint = bc.GetJumpPointForLastInstruction();

            if (_usesGlobalEnv)
            {
                bc.Emit_Load(SymbolRef.Upvalue(WellKnownSymbols.ENV, 0));
                bc.Emit_Store(_env, 0, 0);
                bc.Emit_Pop();
            }

            if (_paramNames.Length > 0)
            {
                bc.Emit_Args(_paramNames);
            }

            _statement.Compile(bc);

            bc.PopSourceRef();
            bc.PushSourceRef(_end);

            bc.Emit_Ret(0);

            bc.LoopTracker.loops.Pop();

            i.NumVal = bc.GetJumpPointForNextInstruction();
            meta.NumVal = bc.GetJumpPointForLastInstruction() - metaip;

            bc.PopSourceRef();

            return entryPoint;
        }

        public int Compile(ByteCode bc, Func<int> afterDecl, string friendlyName)
        {
            using (bc.EnterSource(_begin))
            {
                SymbolRef[] symbs = _closure
                //.Select((s, idx) => s.CloneLocalAndSetFrame(_ClosureFrames[idx]))
                .ToArray();

                _closureInstruction = bc.Emit_Closure(symbs, bc.GetJumpPointForNextInstruction());
                int ops = afterDecl();

                _closureInstruction.NumVal += 2 + ops;
            }

            return CompileBody(bc, friendlyName);
        }

        public override void Compile(ByteCode bc)
        {
            Compile(bc, () => 0, null);
        }
    }
}
