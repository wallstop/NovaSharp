namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Statements;

    /// <summary>
    /// Represents a function frame while the compiler is walking its statements and declaring locals.
    /// </summary>
    internal class BuildTimeScopeFrame
    {
        private readonly BuildTimeScopeBlock _scopeTreeRoot;
        private BuildTimeScopeBlock _scopeTreeHead;
        private readonly RuntimeScopeFrame _scopeFrame = new();

        /// <summary>
        /// Gets a value indicating whether the function reserves the vararg tuple (<c>...</c>).
        /// </summary>
        public bool HasVarArgs { get; private set; }

        /// <summary>
        /// Initializes a new frame with an empty root block.
        /// </summary>
        /// <param name="hasVarArgs">Whether the function accepts <c>...</c>.</param>
        internal BuildTimeScopeFrame(bool hasVarArgs)
        {
            HasVarArgs = hasVarArgs;
            _scopeTreeHead = _scopeTreeRoot = new BuildTimeScopeBlock(null);
        }

        /// <summary>
        /// Pushes a block onto the block tree, making it the active context for subsequent declarations.
        /// </summary>
        internal void PushBlock()
        {
            _scopeTreeHead = _scopeTreeHead.AddChild();
        }

        /// <summary>
        /// Pops the innermost block, ensuring pending gotos are resolved before returning the runtime metadata.
        /// </summary>
        /// <returns>The <see cref="RuntimeScopeBlock"/> associated with the popped block.</returns>
        internal RuntimeScopeBlock PopBlock()
        {
            BuildTimeScopeBlock tree = _scopeTreeHead;

            _scopeTreeHead.ResolveGotos();

            _scopeTreeHead = _scopeTreeHead.Parent;

            if (_scopeTreeHead == null)
            {
                throw new InternalErrorException("Can't pop block - stack underflow");
            }

            return tree.ScopeBlock;
        }

        /// <summary>
        /// Converts the accumulated block tree into the runtime frame description consumed by the VM.
        /// </summary>
        /// <returns>The finalized <see cref="RuntimeScopeFrame"/>.</returns>
        /// <exception cref="InternalErrorException">Thrown when push/pop operations left dangling blocks.</exception>
        internal RuntimeScopeFrame GetRuntimeFrameData()
        {
            if (_scopeTreeHead != _scopeTreeRoot)
            {
                throw new InternalErrorException("Misaligned scope frames/blocks!");
            }

            _scopeFrame.ToFirstBlock = _scopeTreeRoot.ScopeBlock.To;

            return _scopeFrame;
        }

        /// <summary>
        /// Searches the current block stack for a local named <paramref name="name"/>.
        /// </summary>
        internal SymbolRef Find(string name)
        {
            for (BuildTimeScopeBlock tree = _scopeTreeHead; tree != null; tree = tree.Parent)
            {
                SymbolRef l = tree.Find(name);

                if (l != null)
                {
                    return l;
                }
            }

            return null;
        }

        /// <summary>
        /// Declares a local within the active block.
        /// </summary>
        internal SymbolRef DefineLocal(string name, SymbolRefAttributes flags = default)
        {
            return _scopeTreeHead.Define(name, flags);
        }

        /// <summary>
        /// Declares a local, renaming the previous symbol when the name is already used inside the same lexical scope.
        /// </summary>
        internal SymbolRef TryDefineLocal(string name, SymbolRefAttributes flags = default)
        {
            if (_scopeTreeHead.Find(name) != null)
            {
                _scopeTreeHead.Rename(name);
            }

            return _scopeTreeHead.Define(name, flags);
        }

        /// <summary>
        /// Resolves locals/labels/gotos starting from the root block so stack slots are assigned deterministically.
        /// </summary>
        internal void ResolveLRefs()
        {
            _scopeTreeRoot.ResolveGotos();

            _scopeTreeRoot.ResolveLRefs(this);
        }

        /// <summary>
        /// Allocates a debug slot for the provided symbol and returns its index within the frame.
        /// </summary>
        internal int AllocVar(SymbolRef var)
        {
            var.IndexValue = _scopeFrame.DebugSymbols.Count;
            _scopeFrame.DebugSymbols.Add(var);
            return var.IndexValue;
        }

        /// <summary>
        /// Computes the next stack slot offset that would be assigned to a new local.
        /// </summary>
        internal int GetPosForNextVar()
        {
            return _scopeFrame.DebugSymbols.Count;
        }

        /// <summary>
        /// Registers a label within the active block.
        /// </summary>
        internal void DefineLabel(LabelStatement label)
        {
            _scopeTreeHead.DefineLabel(label);
        }

        /// <summary>
        /// Records a pending <c>goto</c> so it can be matched later.
        /// </summary>
        internal void RegisterGoto(GotoStatement gotostat)
        {
            _scopeTreeHead.RegisterGoto(gotostat);
        }
    }
}
