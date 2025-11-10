namespace NovaSharp.Interpreter.Execution.Scopes
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Tree.Statements;

    internal class BuildTimeScopeFrame
    {
        private readonly BuildTimeScopeBlock _scopeTreeRoot;
        private BuildTimeScopeBlock _scopeTreeHead;
        private readonly RuntimeScopeFrame _scopeFrame = new();

        public bool HasVarArgs { get; private set; }

        internal BuildTimeScopeFrame(bool hasVarArgs)
        {
            HasVarArgs = hasVarArgs;
            _scopeTreeHead = _scopeTreeRoot = new BuildTimeScopeBlock(null);
        }

        internal void PushBlock()
        {
            _scopeTreeHead = _scopeTreeHead.AddChild();
        }

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

        internal RuntimeScopeFrame GetRuntimeFrameData()
        {
            if (_scopeTreeHead != _scopeTreeRoot)
            {
                throw new InternalErrorException("Misaligned scope frames/blocks!");
            }

            _scopeFrame.ToFirstBlock = _scopeTreeRoot.ScopeBlock.To;

            return _scopeFrame;
        }

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

        internal SymbolRef DefineLocal(string name)
        {
            return _scopeTreeHead.Define(name);
        }

        internal SymbolRef TryDefineLocal(string name)
        {
            if (_scopeTreeHead.Find(name) != null)
            {
                _scopeTreeHead.Rename(name);
            }

            return _scopeTreeHead.Define(name);
        }

        internal void ResolveLRefs()
        {
            _scopeTreeRoot.ResolveGotos();

            _scopeTreeRoot.ResolveLRefs(this);
        }

        internal int AllocVar(SymbolRef var)
        {
            var.i_Index = _scopeFrame.DebugSymbols.Count;
            _scopeFrame.DebugSymbols.Add(var);
            return var.i_Index;
        }

        internal int GetPosForNextVar()
        {
            return _scopeFrame.DebugSymbols.Count;
        }

        internal void DefineLabel(LabelStatement label)
        {
            _scopeTreeHead.DefineLabel(label);
        }

        internal void RegisterGoto(GotoStatement gotostat)
        {
            _scopeTreeHead.RegisterGoto(gotostat);
        }
    }
}
