namespace NovaSharp.Interpreter.Tree.Statements
{
    using Debugging;
    using Execution;
    using Execution.VM;

    internal class GotoStatement : Statement
    {
        internal SourceRef SourceRef { get; private set; }
        internal Token GotoToken { get; private set; }
        public string Label { get; private set; }

        internal int DefinedVarsCount { get; private set; }
        internal string LastDefinedVarName { get; private set; }

        private Instruction _jump;
        private int _labelAddress = -1;

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
    }
}
