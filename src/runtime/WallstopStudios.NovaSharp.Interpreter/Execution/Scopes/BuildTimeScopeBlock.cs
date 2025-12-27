namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Text;
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
            if (!_definedNames.Remove(name, out SymbolRef sref))
            {
                return;
            }

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.Append('@');
            sb.Append(name);
            sb.Append('_');
            sb.Append(Guid.NewGuid().ToString("N"));
            _definedNames[sb.ToString()] = sref;
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
        /// Tracks the count of locals BEFORE the most recent non-void statement was parsed.
        /// </summary>
        /// <remarks>
        /// Per Lua 5.4 §3.5, the scope of a local variable ends at the "last non-void statement"
        /// of the block. Labels and empty statements are void statements. For labels at the end
        /// of a block, we need to know the var count BEFORE the last non-void statement, since
        /// locals declared in that statement don't have their scope extend to the label.
        /// </remarks>
        private int _varCountBeforeLastNonVoidStatement;
        private string _varNameBeforeLastNonVoidStatement;

        /// <summary>
        /// Tracks the current var count before a potential non-void statement.
        /// Updated at the start of statement parsing.
        /// </summary>
        private int _varCountBeforeCurrentStatement;
        private string _varNameBeforeCurrentStatement;

        private bool _seenNonVoidStatementSinceLastLabel;

        /// <summary>
        /// Tracks labels that may be at the end of the block (no non-void statements after them).
        /// The value is the var count before the last non-void statement was parsed.
        /// </summary>
        private List<(
            LabelStatement label,
            int varCountBeforeLastNonVoid,
            string varNameBeforeLastNonVoid
        )> _labelsAtPotentialEnd;

        /// <summary>
        /// Called BEFORE a statement is parsed to snapshot the current var count.
        /// </summary>
        internal void BeforeStatement()
        {
            _varCountBeforeCurrentStatement = _definedNames.Count;
            _varNameBeforeCurrentStatement = _lastDefinedName;
        }

        /// <summary>
        /// Called when a non-void statement has finished parsing in this block.
        /// </summary>
        /// <remarks>
        /// Non-void statements include everything except labels and empty statements (semicolons).
        /// This tracks state needed for the "void statement" rule in Lua's scoping.
        /// </remarks>
        internal void MarkNonVoidStatement()
        {
            // Use the count from BEFORE this statement started
            _varCountBeforeLastNonVoidStatement = _varCountBeforeCurrentStatement;
            _varNameBeforeLastNonVoidStatement = _varNameBeforeCurrentStatement;
            _seenNonVoidStatementSinceLastLabel = true;
        }

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

                // Track this label as potentially being at end of block.
                // We'll finalize this when the block closes.
                _labelsAtPotentialEnd ??= new List<(LabelStatement, int, string)>();
                _labelsAtPotentialEnd.Add(
                    (label, _varCountBeforeLastNonVoidStatement, _varNameBeforeLastNonVoidStatement)
                );
                _seenNonVoidStatementSinceLastLabel = false;
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
            // Finalize labels that are at the end of the block (followed only by void statements).
            // Per Lua 5.4 §3.5, the scope of locals ends at the "last non-void statement",
            // so labels at the end of a block have a different effective scope.
            FinalizeEndOfBlockLabels();

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
                    // Use the effective var count which accounts for the void statement rule
                    int effectiveVarsCount = label.EffectiveDefinedVarsCount;
                    if (effectiveVarsCount > gotostat.DefinedVarsCount)
                    {
                        throw new SyntaxErrorException(
                            gotostat.GotoToken,
                            "<goto {0}> at line {1} jumps into the scope of local '{2}'",
                            gotostat.Label,
                            gotostat.GotoToken.fromLine,
                            label.EffectiveLastDefinedVarName
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

        /// <summary>
        /// Finalizes labels that were at the end of the block by adjusting their effective scope.
        /// </summary>
        /// <remarks>
        /// Per Lua 5.4 §3.5: "The scope of a local variable begins at the first statement after
        /// its declaration and lasts until the last non-void statement of the innermost block
        /// that includes the declaration. Void statements are labels and empty statements."
        ///
        /// This means if a label is followed only by void statements (other labels, semicolons)
        /// until the end of the block, then locals declared before the label are NOT in scope
        /// at the label's position. We need to track and adjust for this.
        /// </remarks>
        private void FinalizeEndOfBlockLabels()
        {
            if (_labelsAtPotentialEnd == null)
            {
                return;
            }

            // If no non-void statement was seen since the last label, then all labels
            // from that point are at the "end" of the block scope-wise.
            // We iterate in reverse to find which labels are truly at the end.
            // A label is at the end if _seenNonVoidStatementSinceLastLabel is false
            // (no non-void statement after the LAST label), and we propagate backward.

            // Actually, we need to track more carefully: each label records the var count
            // BEFORE the last non-void statement (not after). If no non-void statement
            // followed that label until block end, then the effective var count at that
            // label is the count before the last non-void statement.

            if (!_seenNonVoidStatementSinceLastLabel)
            {
                // No non-void statement was seen after the most recent label(s).
                // All labels that were added since the last non-void statement
                // need their effective scope adjusted.
                foreach (
                    (
                        LabelStatement label,
                        int varCountBeforeLastNonVoid,
                        string varNameBeforeLastNonVoid
                    ) in _labelsAtPotentialEnd
                )
                {
                    // This label is at the end of the block (only void statements follow).
                    // Per the spec, locals declared in the last non-void statement don't
                    // have their scope extend to this label.
                    label.SetEffectiveVars(varCountBeforeLastNonVoid, varNameBeforeLastNonVoid);
                }
            }
        }
    }
}
