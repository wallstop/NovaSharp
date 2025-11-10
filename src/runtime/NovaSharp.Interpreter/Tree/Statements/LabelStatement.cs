namespace NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class LabelStatement : Statement
    {
        public string Label { get; private set; }
        public int Address { get; private set; }
        public SourceRef SourceRef { get; private set; }
        public Token NameToken { get; private set; }

        internal int DefinedVarsCount { get; private set; }
        internal string LastDefinedVarName { get; private set; }

        private readonly List<GotoStatement> _gotos = new();
        private RuntimeScopeBlock _stackFrame;

        public LabelStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            CheckTokenType(lcontext, TokenType.DoubleColon);
            NameToken = CheckTokenType(lcontext, TokenType.Name);
            CheckTokenType(lcontext, TokenType.DoubleColon);

            SourceRef = NameToken.GetSourceRef();
            Label = NameToken.Text;

            lcontext.Scope.DefineLabel(this);
        }

        internal void SetDefinedVars(int definedVarsCount, string lastDefinedVarsName)
        {
            DefinedVarsCount = definedVarsCount;
            LastDefinedVarName = lastDefinedVarsName;
        }

        internal void RegisterGoto(GotoStatement gotostat)
        {
            _gotos.Add(gotostat);
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            bc.Emit_Clean(_stackFrame);

            Address = bc.GetJumpPointForLastInstruction();

            foreach (GotoStatement gotostat in _gotos)
            {
                gotostat.SetAddress(Address);
            }
        }

        internal void SetScope(RuntimeScopeBlock runtimeScopeBlock)
        {
            _stackFrame = runtimeScopeBlock;
        }
    }
}
