namespace WallstopStudios.NovaSharp.Interpreter.Tree
{
    using System;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    /// <summary>
    /// Identifies where an assignment operand was prepared.
    /// </summary>
    internal enum AssignmentOperandKind
    {
        /// <summary>The operand is already on the value stack.</summary>
        Stack,

        /// <summary>The operand is read from a symbol when the store executes.</summary>
        Symbol,

        /// <summary>The operand is a literal carried by the store instruction.</summary>
        Literal,
    }

    /// <summary>
    /// Describes one prepared table or index operand for an assignment target.
    /// </summary>
    internal readonly struct AssignmentOperand
    {
        private AssignmentOperand(AssignmentOperandKind kind, SymbolRef symbol, LuaValue literal)
        {
            Kind = kind;
            Symbol = symbol;
            Literal = literal;
        }

        /// <summary>Gets where the operand is stored.</summary>
        public AssignmentOperandKind Kind { get; }

        /// <summary>Gets the symbol read when the store executes.</summary>
        public SymbolRef Symbol { get; }

        /// <summary>Gets the literal carried by the store instruction.</summary>
        public LuaValue Literal { get; }

        /// <summary>Creates an operand already present on the value stack.</summary>
        public static AssignmentOperand Stack()
        {
            return new AssignmentOperand(AssignmentOperandKind.Stack, null, default);
        }

        /// <summary>Creates an operand read from a symbol when the store executes.</summary>
        public static AssignmentOperand FromSymbol(SymbolRef symbol)
        {
            return new AssignmentOperand(AssignmentOperandKind.Symbol, symbol, default);
        }

        /// <summary>Creates a literal operand carried by the store instruction.</summary>
        public static AssignmentOperand FromLiteral(LuaValue literal)
        {
            return new AssignmentOperand(AssignmentOperandKind.Literal, null, literal);
        }
    }

    /// <summary>
    /// Describes one left-hand side of an assignment after its evaluated operands have been prepared.
    /// </summary>
    internal sealed class AssignmentTarget
    {
        private AssignmentTarget(
            IVariable directVariable,
            AssignmentOperand baseOperand,
            AssignmentOperand indexOperand,
            bool isNameIndex,
            bool isExpList,
            string baseName
        )
        {
            DirectVariable = directVariable;
            BaseOperand = baseOperand;
            IndexOperand = indexOperand;
            IsNameIndex = isNameIndex;
            IsExpList = isExpList;
            BaseName = baseName;
        }

        /// <summary>Gets the direct variable target, or null for a table target.</summary>
        public IVariable DirectVariable { get; }

        /// <summary>Gets the prepared table operand.</summary>
        public AssignmentOperand BaseOperand { get; }

        /// <summary>Gets the prepared index operand.</summary>
        public AssignmentOperand IndexOperand { get; }

        /// <summary>Gets whether the index is the literal field name.</summary>
        public bool IsNameIndex { get; }

        /// <summary>Gets whether the index expression was a comma-separated list.</summary>
        public bool IsExpList { get; }

        /// <summary>Gets the diagnostic base name for index errors.</summary>
        public string BaseName { get; }

        /// <summary>Gets the prepared table operand's stack slot, or -1.</summary>
        public int BaseStackSlot { get; private set; } = -1;

        /// <summary>Gets the prepared index operand's stack slot, or -1.</summary>
        public int IndexStackSlot { get; private set; } = -1;

        /// <summary>Creates a target stored directly in a variable.</summary>
        public static AssignmentTarget Direct(IVariable variable)
        {
            return new AssignmentTarget(variable, default, default, false, false, null);
        }

        /// <summary>Creates a target stored through prepared table operands.</summary>
        public static AssignmentTarget Indexed(
            AssignmentOperand baseOperand,
            AssignmentOperand indexOperand,
            bool isNameIndex,
            bool isExpList,
            string baseName
        )
        {
            return new AssignmentTarget(
                null,
                baseOperand,
                indexOperand,
                isNameIndex,
                isExpList,
                baseName
            );
        }

        /// <summary>Assigns this target's prepared operands to slots in the target stack region.</summary>
        public void ReserveStackSlots(ref int nextSlot)
        {
            if (DirectVariable != null)
            {
                return;
            }

            if (BaseOperand.Kind == AssignmentOperandKind.Stack)
            {
                BaseStackSlot = nextSlot++;
            }

            if (IndexOperand.Kind == AssignmentOperandKind.Stack)
            {
                IndexStackSlot = nextSlot++;
            }
        }

        /// <summary>
        /// Determines whether a later direct assignment must copy the supplied symbol for this
        /// target's still-deferred table or index operand.
        /// </summary>
        public bool NeedsConflictSnapshot(SymbolRef symbol)
        {
            if (DirectVariable != null)
            {
                return false;
            }

            return IsDeferredOperand(BaseOperand, symbol, BaseStackSlot)
                || IsDeferredOperand(IndexOperand, symbol, IndexStackSlot);
        }

        /// <summary>
        /// Points this target's deferred operands at a conflict copy emitted for a later direct
        /// assignment.
        /// </summary>
        public void ApplyConflictSnapshot(SymbolRef symbol, int stackSlot)
        {
            if (IsDeferredOperand(BaseOperand, symbol, BaseStackSlot))
            {
                BaseStackSlot = stackSlot;
            }

            if (IsDeferredOperand(IndexOperand, symbol, IndexStackSlot))
            {
                IndexStackSlot = stackSlot;
            }
        }

        private static bool IsDeferredOperand(
            AssignmentOperand operand,
            SymbolRef symbol,
            int stackSlot
        )
        {
            return operand.Kind == AssignmentOperandKind.Symbol
                && stackSlot < 0
                && IsSameSymbol(operand.Symbol, symbol);
        }

        /// <summary>
        /// Compares symbols by identity or by the upvalue slot they resolve to. Upvalue references
        /// are recreated per lookup, so reference equality alone misses assignment conflicts.
        /// </summary>
        public static bool IsSameSymbol(SymbolRef left, SymbolRef right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Type != right.Type)
            {
                return false;
            }

            return left.IndexValue == right.IndexValue
                && string.Equals(left.NameValue, right.NameValue, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether an assignment may read the symbol when the store executes rather than
        /// snapshotting it before the right-hand side. Direct locals and Lua 5.2+ upvalue tables can
        /// stay register-like; Lua 5.1 upvalues and upvalue keys are evaluated up front by the
        /// reference compiler.
        /// </summary>
        public static bool CanDeferSymbolRead(
            SymbolRef symbol,
            ByteCode bc,
            bool isIndexOperand,
            bool hasConstantIndex
        )
        {
            switch (symbol.Type)
            {
                case SymbolRefType.Local:
                    return true;
                case SymbolRefType.DefaultEnv:
                    return true;
                case SymbolRefType.UpValue:
                    if (isIndexOperand)
                    {
                        return false;
                    }

                    LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                        bc.Script.CompatibilityVersion
                    );
                    if (version == LuaCompatibilityVersion.Lua51)
                    {
                        return false;
                    }

                    return hasConstantIndex || version < LuaCompatibilityVersion.Lua54;
                default:
                    return false;
            }
        }
    }
}
