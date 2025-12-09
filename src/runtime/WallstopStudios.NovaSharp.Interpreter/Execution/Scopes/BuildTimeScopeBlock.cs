namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Statements;

    /// <summary>
    /// Represents a single lexical block during compilation and tracks the locals, labels, and gotos declared within it.
    /// </summary>
    /// <remarks>
    /// Lua 5.4 (§3.3.4 and §3.5) requires that blocks record the locals that are live when a <c>goto</c> crosses block
    /// boundaries. This helper mirrors that behaviour so the compiler can reject invalid jumps and emit correct lifetime data.
    /// </remarks>
    internal class BuildTimeScopeBlock
    {
        /// <summary>
        /// Gets the parent block, or <c>null</c> for the root function body.
        /// </summary>
        internal BuildTimeScopeBlock Parent { get; private set; }

        /// <summary>
        /// Gets the child blocks nested inside the current lexical region.
        /// </summary>
        internal List<BuildTimeScopeBlock> ChildNodes { get; private set; }

        /// <summary>
        /// Gets the runtime metadata produced for this block once locals are resolved.
        /// </summary>
        internal RuntimeScopeBlock ScopeBlock { get; private set; }

        private readonly Dictionary<string, SymbolRef> _definedNames = new();
        private readonly List<SymbolRef> _localsInOrder = new();
        private readonly List<SymbolRef> _toBeClosed = new();

        /// <summary>
        /// Renames the most recently declared symbol so a re-declaration in the same block can coexist (needed for goto validation).
        /// </summary>
        /// <param name="name">Original name of the local.</param>
        internal void Rename(string name)
        {
            SymbolRef sref = _definedNames[name];
            _definedNames.Remove(name);
            _definedNames.Add($"@{name}_{Guid.NewGuid().ToString("N")}", sref);
        }

        /// <summary>
        /// Initializes a block node and associates it with its parent.
        /// </summary>
        /// <param name="parent">Parent block, or <c>null</c> for the root frame.</param>
        internal BuildTimeScopeBlock(BuildTimeScopeBlock parent)
        {
            Parent = parent;
            ChildNodes = new List<BuildTimeScopeBlock>();
            ScopeBlock = new RuntimeScopeBlock();
        }

        /// <summary>
        /// Creates a child block and makes it the tail of the nested tree.
        /// </summary>
        /// <returns>The newly created block.</returns>
        internal BuildTimeScopeBlock AddChild()
        {
            BuildTimeScopeBlock block = new(this);
            ChildNodes.Add(block);
            return block;
        }

        /// <summary>
        /// Looks up a local by name within the current block only.
        /// </summary>
        /// <param name="name">Local name.</param>
        /// <returns>The matching symbol or <c>null</c>.</returns>
        internal SymbolRef Find(string name)
        {
            return _definedNames.GetOrDefault(name);
        }

        /// <summary>
        /// Declares a local inside the block, preserving insertion order so slots can be assigned deterministically.
        /// </summary>
        /// <param name="name">Local name.</param>
        /// <param name="attributes">Attributes assigned to the symbol.</param>
        /// <returns>The declared symbol.</returns>
        internal SymbolRef Define(string name, SymbolRefAttributes attributes)
        {
            SymbolRef l = SymbolRef.Local(name, -1, attributes);
            _definedNames.Add(name, l);
            _localsInOrder.Add(l);

            if ((attributes & SymbolRefAttributes.ToBeClosed) != 0)
            {
                _toBeClosed.Add(l);
            }

            _lastDefinedName = name;
            return l;
        }

        /// <summary>
        /// Assigns stack slots to all locals in the block and propagates metadata to child blocks.
        /// </summary>
        /// <param name="buildTimeScopeFrame">Frame that owns the locals.</param>
        /// <returns>The highest slot index that remains live after visiting this block.</returns>
        internal int ResolveLRefs(BuildTimeScopeFrame buildTimeScopeFrame)
        {
            int firstVal = -1;
            int lastVal = -1;

            foreach (SymbolRef lref in _localsInOrder)
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

            if (_toBeClosed.Count > 0)
            {
                ScopeBlock.ToBeClosed = _toBeClosed.ToArray();
            }
            else
            {
                ScopeBlock.ToBeClosed = Array.Empty<SymbolRef>();
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

        /// <summary>
        /// Registers a label so subsequent gotos can reference it, validating duplicates per Lua 5.4 §3.3.4.
        /// </summary>
        /// <param name="label">Label being declared.</param>
        /// <exception cref="SyntaxErrorException">Thrown when a label is defined twice in the same scope.</exception>
        internal void DefineLabel(LabelStatement label)
        {
            _localLabels ??= new Dictionary<string, LabelStatement>();

            if (_localLabels.TryGetValue(label.Label, out LabelStatement existing))
            {
                throw new SyntaxErrorException(
                    label.NameToken,
                    "label '{0}' already defined on line {1}",
                    label.Label,
                    existing.SourceRef.FromLine
                );
            }
            else
            {
                _localLabels.Add(label.Label, label);
                label.SetDefinedVars(_definedNames.Count, _lastDefinedName);
                label.SetDeclaringBlock(this);
            }
        }

        /// <summary>
        /// Records a <c>goto</c> encountered inside the block so it can be bound to a label after parsing finishes.
        /// </summary>
        /// <param name="gotostat">The jump statement.</param>
        internal void RegisterGoto(GotoStatement gotostat)
        {
            if (_pendingGotos == null)
            {
                _pendingGotos = new List<GotoStatement>();
            }

            gotostat.SetDeclaringBlock(this);

            _pendingGotos.Add(gotostat);
            gotostat.SetDefinedVars(_definedNames.Count, _lastDefinedName);
        }

        /// <summary>
        /// Links pending gotos with their target labels or bubbles them up to the parent block when the label is not local.
        /// </summary>
        /// <exception cref="SyntaxErrorException">Thrown when a goto skips over locals or when the label is missing.</exception>
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
                            gotostat.GotoToken.FromLine,
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
                            gotostat.GotoToken.FromLine
                        );
                    }

                    Parent.RegisterGoto(gotostat);
                }
            }

            _pendingGotos.Clear();
        }
    }
}
