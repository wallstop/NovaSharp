namespace NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using Debugging;
    using Expressions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class FunctionDefinitionStatement : Statement
    {
        private readonly SymbolRef _funcSymbol;
        private readonly SourceRef _sourceRef;

        private readonly bool _local;
        private readonly bool _isMethodCallingConvention;
        private readonly string _methodName;

        private readonly string _friendlyName;
        private readonly List<string> _tableAccessors;
        private readonly FunctionDefinitionExpression _funcDef;

        public FunctionDefinitionStatement(
            ScriptLoadingContext lcontext,
            bool local,
            Token localToken
        )
            : base(lcontext)
        {
            // here lexer must be at the 'function' keyword
            Token funcKeyword = CheckTokenType(lcontext, TokenType.Function);
            funcKeyword = localToken ?? funcKeyword; // for debugger purposes

            _local = local;

            if (_local)
            {
                Token name = CheckTokenType(lcontext, TokenType.Name);
                _funcSymbol = lcontext.Scope.TryDefineLocal(name.Text);
                _friendlyName = $"{name.Text} (local)";
                _sourceRef = funcKeyword.GetSourceRef(name);
            }
            else
            {
                Token name = CheckTokenType(lcontext, TokenType.Name);
                string firstName = name.Text;

                _sourceRef = funcKeyword.GetSourceRef(name);

                _funcSymbol = lcontext.Scope.Find(firstName);
                _friendlyName = firstName;

                if (lcontext.Lexer.Current.Type != TokenType.BrkOpenRound)
                {
                    _tableAccessors = new List<string>();

                    while (lcontext.Lexer.Current.Type != TokenType.BrkOpenRound)
                    {
                        Token separator = lcontext.Lexer.Current;

                        if (separator.Type != TokenType.Colon && separator.Type != TokenType.Dot)
                        {
                            UnexpectedTokenType(separator);
                        }

                        lcontext.Lexer.Next();

                        Token field = CheckTokenType(lcontext, TokenType.Name);

                        _friendlyName += separator.Text + field.Text;
                        _sourceRef = funcKeyword.GetSourceRef(field);

                        if (separator.Type == TokenType.Colon)
                        {
                            _methodName = field.Text;
                            _isMethodCallingConvention = true;
                            break;
                        }
                        else
                        {
                            _tableAccessors.Add(field.Text);
                        }
                    }

                    if (_methodName == null && _tableAccessors.Count > 0)
                    {
                        _methodName = _tableAccessors[^1];
                        _tableAccessors.RemoveAt(_tableAccessors.Count - 1);
                    }
                }
            }

            _funcDef = new FunctionDefinitionExpression(
                lcontext,
                _isMethodCallingConvention,
                false
            );
            lcontext.Source.Refs.Add(_sourceRef);
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            using (bc.EnterSource(_sourceRef))
            {
                if (_local)
                {
                    bc.Emit_Literal(DynValue.Nil);
                    bc.Emit_Store(_funcSymbol, 0, 0);
                    _funcDef.Compile(bc, () => SetFunction(bc, 2), _friendlyName);
                }
                else if (_methodName == null)
                {
                    _funcDef.Compile(bc, () => SetFunction(bc, 1), _friendlyName);
                }
                else
                {
                    _funcDef.Compile(bc, () => SetMethod(bc), _friendlyName);
                }
            }
        }

        private int SetMethod(Execution.VM.ByteCode bc)
        {
            int cnt = 0;

            cnt += bc.Emit_Load(_funcSymbol);

            foreach (string str in _tableAccessors)
            {
                bc.Emit_Index(DynValue.NewString(str), true);
                cnt += 1;
            }

            bc.Emit_IndexSet(0, 0, DynValue.NewString(_methodName), true);

            return 1 + cnt;
        }

        private int SetFunction(Execution.VM.ByteCode bc, int numPop)
        {
            int num = bc.Emit_Store(_funcSymbol, 0, 0);
            bc.Emit_Pop(numPop);
            return num + 1;
        }
    }
}
