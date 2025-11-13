namespace NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using Debugging;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class GotoStatement : Statement
    {
        internal SourceRef SourceRef { get; private set; }
        internal Token GotoToken { get; private set; }
        public string Label { get; private set; }

        internal int DefinedVarsCount { get; private set; }
        internal string LastDefinedVarName { get; private set; }

        private Instruction _jump;
        private int _labelAddress = -1;
        private BuildTimeScopeBlock _declaringBlock;
        private List<RuntimeScopeBlock> _exitScopes;

        public GotoStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            GotoToken = CheckTokenType(lcontext, TokenType.Goto);
            Token name = CheckTokenType(lcontext, TokenType.Name);

            SourceRef = GotoToken.GetSourceRef(name);

            Label = name.Text;

            lcontext.Scope.RegisterGoto(this);
        }

        public override void Compile(ByteCode bc)
        {
            if (_exitScopes != null)
            {
                foreach (RuntimeScopeBlock scope in _exitScopes)
                {
                    bc.Emit_Exit(scope);
                }
            }

            _jump = bc.Emit_Jump(OpCode.Jump, _labelAddress);
        }

        internal void SetDefinedVars(int definedVarsCount, string lastDefinedVarsName)
        {
            DefinedVarsCount = definedVarsCount;
            LastDefinedVarName = lastDefinedVarsName;
        }

        internal void SetAddress(int labelAddress)
        {
            _labelAddress = labelAddress;

            if (_jump != null)
            {
                _jump.NumVal = labelAddress;
            }
        }

        internal void SetDeclaringBlock(BuildTimeScopeBlock block)
        {
            if (_declaringBlock == null)
            {
                _declaringBlock = block;
            }
        }

        internal BuildTimeScopeBlock GetDeclaringBlock()
        {
            return _declaringBlock;
        }

        internal void SetExitScopes(List<RuntimeScopeBlock> scopes)
        {
            _exitScopes = scopes;
        }
    }
}
