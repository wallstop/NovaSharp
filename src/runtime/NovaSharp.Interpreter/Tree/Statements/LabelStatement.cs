namespace NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a Lua label (`::name::`) that can be targeted by <c>goto</c>.
    /// </summary>
    internal class LabelStatement : Statement
    {
        /// <summary>
        /// Label name exposed to goto statements.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Bytecode address patched once the label compiles.
        /// </summary>
        public int Address { get; private set; }

        /// <summary>
        /// Source span covering the label declaration.
        /// </summary>
        public SourceRef SourceRef { get; private set; }

        /// <summary>
        /// Token storing the label identifier for diagnostics.
        /// </summary>
        public Token NameToken { get; private set; }

        /// <summary>
        /// Number of locals defined before the label.
        /// </summary>
        internal int DefinedVarsCount { get; private set; }

        /// <summary>
        /// Last variable defined before the label (used for goto validation).
        /// </summary>
        internal string LastDefinedVarName { get; private set; }

        private readonly List<GotoStatement> _gotos = new();
        private RuntimeScopeBlock _stackFrame;
        private BuildTimeScopeBlock _declaringBlock;

        /// <summary>
        /// Parses a label and registers it with the current scope.
        /// </summary>
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

        /// <summary>
        /// Records how many locals existed when the label was declared.
        /// </summary>
        /// <param name="definedVarsCount">Number of locals active at the label site.</param>
        /// <param name="lastDefinedVarsName">Most recently defined local name.</param>
        internal void SetDefinedVars(int definedVarsCount, string lastDefinedVarsName)
        {
            DefinedVarsCount = definedVarsCount;
            LastDefinedVarName = lastDefinedVarsName;
        }

        /// <summary>
        /// Attaches a goto to this label so the compiler can patch its jump later.
        /// </summary>
        /// <param name="gotostat">Goto targeting this label.</param>
        internal void RegisterGoto(GotoStatement gotostat)
        {
            _gotos.Add(gotostat);
            List<RuntimeScopeBlock> exitScopes = BuildExitScopes(
                gotostat.GetDeclaringBlock(),
                _declaringBlock
            );
            gotostat.SetExitScopes(exitScopes);
        }

        /// <summary>
        /// Emits cleanup for the declaring scope, records the bytecode address, and patches pending gotos.
        /// </summary>
        public override void Compile(Execution.VM.ByteCode bc)
        {
            bc.EmitClean(_stackFrame);

            Address = bc.GetJumpPointForLastInstruction();

            foreach (GotoStatement gotostat in _gotos)
            {
                gotostat.SetAddress(Address);
            }
        }

        /// <summary>
        /// Stores the runtime scope owning this label; used when emitting cleanup instructions.
        /// </summary>
        internal void SetScope(RuntimeScopeBlock runtimeScopeBlock)
        {
            _stackFrame = runtimeScopeBlock;
        }

        /// <summary>
        /// Records the build-time block that introduced this label, enabling goto validation.
        /// </summary>
        internal void SetDeclaringBlock(BuildTimeScopeBlock block)
        {
            _declaringBlock = block;
        }

        /// <summary>
        /// Determines which runtime scopes must be exited when moving from a goto's declaring block to this label's block.
        /// </summary>
        /// <param name="gotoBlock">Build-time block containing the goto.</param>
        /// <param name="labelBlock">Build-time block containing the label.</param>
        /// <returns>Ordered list of runtime scopes to exit; empty when crossing is illegal.</returns>
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
