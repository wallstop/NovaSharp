namespace NovaSharp.Interpreter.Execution.Scopes
{
    using System;
    using System.Collections.Generic;
    using Tree.Statements;

    internal class BuildTimeScopeBlock
    {
        internal BuildTimeScopeBlock Parent { get; private set; }
        internal List<BuildTimeScopeBlock> ChildNodes { get; private set; }

        internal RuntimeScopeBlock ScopeBlock { get; private set; }

        private readonly Dictionary<string, SymbolRef> _definedNames = new();

        internal void Rename(string name)
        {
            SymbolRef sref = _definedNames[name];
            _definedNames.Remove(name);
            _definedNames.Add($"@{name}_{Guid.NewGuid().ToString("N")}", sref);
        }

        internal BuildTimeScopeBlock(BuildTimeScopeBlock parent)
        {
            Parent = parent;
            ChildNodes = new List<BuildTimeScopeBlock>();
            ScopeBlock = new RuntimeScopeBlock();
        }

        internal BuildTimeScopeBlock AddChild()
        {
            BuildTimeScopeBlock block = new(this);
            ChildNodes.Add(block);
            return block;
        }

        internal SymbolRef Find(string name)
        {
            return _definedNames.GetOrDefault(name);
        }

        internal SymbolRef Define(string name)
        {
            SymbolRef l = SymbolRef.Local(name, -1);
            _definedNames.Add(name, l);
            _lastDefinedName = name;
            return l;
        }

        internal int ResolveLRefs(BuildTimeScopeFrame buildTimeScopeFrame)
        {
            int firstVal = -1;
            int lastVal = -1;

            foreach (SymbolRef lref in _definedNames.Values)
            {
                int pos = buildTimeScopeFrame.AllocVar(lref);

                if (firstVal < 0)
                {
                    firstVal = pos;
                }

                lastVal = pos;
            }

            ScopeBlock.From = firstVal;
            ScopeBlock.ToInclusive = ScopeBlock.To = lastVal;

            if (firstVal < 0)
            {
                ScopeBlock.From = buildTimeScopeFrame.GetPosForNextVar();
            }

            foreach (BuildTimeScopeBlock child in ChildNodes)
            {
                ScopeBlock.ToInclusive = Math.Max(
                    ScopeBlock.ToInclusive,
                    child.ResolveLRefs(buildTimeScopeFrame)
                );
            }

            if (_localLabels != null)
            {
                foreach (LabelStatement label in _localLabels.Values)
                {
                    label.SetScope(ScopeBlock);
                }
            }

            return ScopeBlock.ToInclusive;
        }

        private List<GotoStatement> _pendingGotos;
        private Dictionary<string, LabelStatement> _localLabels;
        private string _lastDefinedName;

        internal void DefineLabel(LabelStatement label)
        {
            if (_localLabels == null)
            {
                _localLabels = new Dictionary<string, LabelStatement>();
            }

            if (_localLabels.ContainsKey(label.Label))
            {
                throw new SyntaxErrorException(
                    label.NameToken,
                    "label '{0}' already defined on line {1}",
                    label.Label,
                    _localLabels[label.Label].SourceRef.FromLine
                );
            }
            else
            {
                _localLabels.Add(label.Label, label);
                label.SetDefinedVars(_definedNames.Count, _lastDefinedName);
            }
        }

        internal void RegisterGoto(GotoStatement gotostat)
        {
            if (_pendingGotos == null)
            {
                _pendingGotos = new List<GotoStatement>();
            }

            _pendingGotos.Add(gotostat);
            gotostat.SetDefinedVars(_definedNames.Count, _lastDefinedName);
        }

        internal void ResolveGotos()
        {
            if (_pendingGotos == null)
            {
                return;
            }

            foreach (GotoStatement gotostat in _pendingGotos)
            {
                if (
                    _localLabels != null
                    && _localLabels.TryGetValue(gotostat.Label, out LabelStatement label)
                )
                {
                    if (label.DefinedVarsCount > gotostat.DefinedVarsCount)
                    {
                        throw new SyntaxErrorException(
                            gotostat.GotoToken,
                            "<goto {0}> at line {1} jumps into the scope of local '{2}'",
                            gotostat.Label,
                            gotostat.GotoToken.fromLine,
                            label.LastDefinedVarName
                        );
                    }

                    label.RegisterGoto(gotostat);
                }
                else
                {
                    if (Parent == null)
                    {
                        throw new SyntaxErrorException(
                            gotostat.GotoToken,
                            "no visible label '{0}' for <goto> at line {1}",
                            gotostat.Label,
                            gotostat.GotoToken.fromLine
                        );
                    }

                    Parent.RegisterGoto(gotostat);
                }
            }

            _pendingGotos.Clear();
        }
    }
}
