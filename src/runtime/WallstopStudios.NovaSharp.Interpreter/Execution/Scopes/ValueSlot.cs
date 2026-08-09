namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using System.Runtime.CompilerServices;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Inline storage for a Lua local variable, promoted to a heap cell only when captured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A default slot is inactive and reads as <see cref="LuaValue.Nil"/>. Assignment activates the
    /// slot without allocating. Capturing promotes it to an <see cref="UpvalueCell"/> shared by the
    /// frame and every closure over that local.
    /// </para>
    /// <para>
    /// The explicit active state preserves the distinction between an out-of-scope local and an
    /// in-scope local whose value is nil. Clearing a pooled local-scope array restores the inactive
    /// default and drops the frame's reference to any escaped cell without mutating that cell.
    /// </para>
    /// </remarks>
    internal struct ValueSlot
    {
        private LuaValue _inlineValue;
        private UpvalueCell _capturedCell;
        private bool _isActive;

        /// <summary>
        /// Initializes an active slot holding the specified value.
        /// </summary>
        /// <param name="value">The initial value.</param>
        internal ValueSlot(LuaValue value)
        {
            _inlineValue = value;
            _capturedCell = null;
            _isActive = true;
        }

        /// <summary>
        /// Gets whether this local is currently in scope.
        /// </summary>
        internal bool IsActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isActive; }
        }

        /// <summary>
        /// Gets the value currently held by this slot. Inactive slots read as nil.
        /// </summary>
        internal LuaValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _capturedCell?.Value ?? _inlineValue; }
        }

        /// <summary>
        /// Activates the local and assigns its value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Assign(LuaValue value)
        {
            _isActive = true;
            if (_capturedCell == null)
            {
                _inlineValue = value;
            }
            else
            {
                _capturedCell.Value = value;
            }
        }

        /// <summary>
        /// Activates this local and returns its stable captured cell.
        /// </summary>
        internal UpvalueCell Capture()
        {
            _isActive = true;
            return _capturedCell ??= new UpvalueCell(_inlineValue);
        }

        /// <summary>
        /// Marks the frame-local slot out of scope without mutating an escaped captured cell.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Deactivate()
        {
            this = default;
        }
    }

    /// <summary>
    /// Heap identity shared by closures which capture the same Lua local.
    /// </summary>
    internal sealed class UpvalueCell
    {
        internal UpvalueCell(LuaValue value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets or sets the value shared by every closure over this cell.
        /// </summary>
        internal LuaValue Value { get; set; }
    }
}
