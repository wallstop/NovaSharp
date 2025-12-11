namespace WallstopStudios.NovaSharp.Interpreter.Tree.Statements
{
    using System.Collections.Generic;
    using Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a Lua <c>goto</c> statement, tracking scope/variable metadata required by §3.3.4.
    /// </summary>
    internal class GotoStatement : Statement
    {
        /// <summary>
        /// Source span covering the <c>goto</c> statement.
        /// </summary>
        internal SourceRef SourceRef { get; private set; }

        /// <summary>
        /// Token representing the <c>goto</c> keyword; used for diagnostics when relabelling fails.
        /// </summary>
        internal Token GotoToken { get; private set; }

        /// <summary>
        /// Name of the label targeted by this goto.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Number of locals defined prior to this goto (used to validate forward-jump rules).
        /// </summary>
        internal int DefinedVarsCount { get; private set; }

        /// <summary>
        /// Name of the last variable defined before encountering this goto.
        /// </summary>
        internal string LastDefinedVarName { get; private set; }

        private Instruction _jump;
        private int _labelAddress = -1;
        private BuildTimeScopeBlock _declaringBlock;
        private List<RuntimeScopeBlock> _exitScopes;

        /// <summary>
        /// Parses a <c>goto</c> statement and registers it with the current scope so label resolution can occur later.
        /// </summary>
        public GotoStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            GotoToken = CheckTokenType(lcontext, TokenType.Goto);
            Token name = CheckTokenType(lcontext, TokenType.Name);

            SourceRef = GotoToken.GetSourceRef(name);

            Label = name.Text;

            lcontext.Scope.RegisterGoto(this);
        }

        /// <summary>
        /// Emits the pending jump; scope exits are injected if the goto needs to unwind runtime blocks.
        /// </summary>
        /// <param name="bc">Bytecode builder receiving the generated jump.</param>
        public override void Compile(ByteCode bc)
        {
            if (_exitScopes != null)
            {
                foreach (RuntimeScopeBlock scope in _exitScopes)
                {
                    bc.EmitExit(scope);
                }
            }

            _jump = bc.EmitJump(OpCode.Jump, _labelAddress);
        }

        /// <summary>
        /// Records how many local variables existed at the declaration point; used to enforce Lua's goto restrictions.
        /// </summary>
        /// <param name="definedVarsCount">Number of locals active when the goto was parsed.</param>
        /// <param name="lastDefinedVarsName">Name of the most recent local defined at that point.</param>
        internal void SetDefinedVars(int definedVarsCount, string lastDefinedVarsName)
        {
            DefinedVarsCount = definedVarsCount;
            LastDefinedVarName = lastDefinedVarsName;
        }

        /// <summary>
        /// Assigns the bytecode address of the resolved label and updates previously emitted jumps.
        /// </summary>
        /// <param name="labelAddress">Instruction pointer corresponding to the label.</param>
        internal void SetAddress(int labelAddress)
        {
            _labelAddress = labelAddress;

            if (_jump != null)
            {
                _jump.NumVal = labelAddress;
            }
        }

        /// <summary>
        /// Captures the declaring build-time scope block so late binding can validate crossing restrictions.
        /// </summary>
        /// <param name="block">Build-time block that owns the statement.</param>
        internal void SetDeclaringBlock(BuildTimeScopeBlock block)
        {
            if (_declaringBlock == null)
            {
                _declaringBlock = block;
            }
        }

        /// <summary>
        /// Returns the build-time block that owns this goto.
        /// </summary>
        /// <returns>The recorded declaring block.</returns>
        internal BuildTimeScopeBlock GetDeclaringBlock()
        {
            return _declaringBlock;
        }

        /// <summary>
        /// Specifies the runtime scopes that must be exited when jumping to the target label.
        /// </summary>
        /// <param name="scopes">Scopes that should be unwound before the jump lands.</param>
        internal void SetExitScopes(List<RuntimeScopeBlock> scopes)
        {
            _exitScopes = scopes;
        }
    }
}
