namespace WallstopStudios.NovaSharp.Interpreter.Interop
{
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Optional compatibility capability for self-describing userdata that can distinguish an
    /// unsupported lookup from a supported lookup whose value is explicitly
    /// <see cref="DynValue.Nil" /> or <see cref="DynValue.Void" />.
    /// </summary>
    /// <remarks>
    /// This interface complements <see cref="IUserDataType" /> without changing its legacy
    /// null-return contract. Implementations must set the output value to <see cref="DynValue.Nil" />
    /// when returning <see langword="false" />. A successful lookup must return a non-null value.
    /// </remarks>
    public interface IUserDataTypeTryAccess : IUserDataType
    {
        /// <summary>
        /// Attempts to perform an index get operation.
        /// </summary>
        public bool TryIndex(
            Script script,
            DynValue index,
            bool isDirectIndexing,
            out DynValue value
        );

        /// <summary>
        /// Attempts to resolve a userdata metamethod.
        /// </summary>
        public bool TryMetaIndex(Script script, string metaname, out DynValue value);
    }
}
