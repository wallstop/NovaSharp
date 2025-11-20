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
        private BuildTimeScopeBlock _declaringBlock;

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
            List<RuntimeScopeBlock> exitScopes = BuildExitScopes(
                gotostat.GetDeclaringBlock(),
                _declaringBlock
            );
            gotostat.SetExitScopes(exitScopes);
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            bc.EmitClean(_stackFrame);

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

        internal void SetDeclaringBlock(BuildTimeScopeBlock block)
        {
            _declaringBlock = block;
        }

        internal static List<RuntimeScopeBlock> BuildExitScopes(
            BuildTimeScopeBlock gotoBlock,
            BuildTimeScopeBlock labelBlock
        )
        {
            List<RuntimeScopeBlock> scopes = new();

            if (gotoBlock == null || labelBlock == null)
            {
                return scopes;
            }

            BuildTimeScopeBlock walker = gotoBlock;

            while (walker != null && walker != labelBlock)
            {
                if (walker.ScopeBlock != null)
                {
                    scopes.Add(walker.ScopeBlock);
                }

                walker = walker.Parent;
            }

            if (walker != labelBlock)
            {
                scopes.Clear();
            }

            return scopes;
        }
    }
}
