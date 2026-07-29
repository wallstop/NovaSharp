namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using System.Runtime.CompilerServices;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// A mutable storage cell holding the current value of a Lua local variable or upvalue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the "slot" half of the slot/value split: a slot is the mutable identity that a
    /// closure captures, while the <see cref="DynValue"/> it holds is an immutable value that can
    /// be shared freely. Before the split, a local was itself a mutable <see cref="DynValue"/>, so
    /// every read had to clone defensively (<c>AsReadOnly()</c>) to prevent later assignments from
    /// retroactively changing values already pushed onto the value stack or stored in tables.
    /// </para>
    /// <para>
    /// With the split, reading a local or upvalue is a plain field load with no allocation, and
    /// capturing a local in a closure simply shares this cell.
    /// </para>
    /// </remarks>
    internal sealed class ValueSlot
    {
        private DynValue _value;

        /// <summary>
        /// Initializes a new slot holding <see cref="DynValue.Nil"/>.
        /// </summary>
        internal ValueSlot()
        {
            _value = DynValue.Nil;
        }

        /// <summary>
        /// Initializes a new slot holding the specified value.
        /// </summary>
        /// <param name="value">The initial value; <c>null</c> is normalized to <see cref="DynValue.Nil"/>.</param>
        internal ValueSlot(DynValue value)
        {
            _value = value ?? DynValue.Nil;
        }

        /// <summary>
        /// Gets or sets the value currently held by this slot. Never <c>null</c>.
        /// </summary>
        internal DynValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _value; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _value = value ?? DynValue.Nil; }
        }
    }
}
