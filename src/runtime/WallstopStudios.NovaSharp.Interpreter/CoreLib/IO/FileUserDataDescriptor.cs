namespace WallstopStudios.NovaSharp.Interpreter.CoreLib.IO
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop;

    /// <summary>
    /// Wraps the default file userdata descriptor to align Lua semantics for numeric indexing.
    /// </summary>
    internal sealed class FileUserDataDescriptor : IUserDataDescriptorTryAccess
    {
        private readonly IUserDataDescriptor _inner;

        internal FileUserDataDescriptor(IUserDataDescriptor inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public string Name => _inner.Name;

        public Type Type => _inner.Type;

        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            return TryIndex(script, obj, index, isDirectIndexing, out DynValue value)
                ? value
                : null;
        }

        public bool TryIndex(
            Script script,
            object obj,
            DynValue index,
            bool isDirectIndexing,
            out DynValue value
        )
        {
            DynValue scalar = index?.ToScalar();
            if (scalar != null && scalar.Type != DataType.String)
            {
                value = DynValue.Nil;
                return true;
            }

            return UserDataAccess.TryIndex(_inner, script, obj, index, isDirectIndexing, out value);
        }

        public bool SetIndex(
            Script script,
            object obj,
            DynValue index,
            DynValue value,
            bool isDirectIndexing
        )
        {
            DynValue scalar = index?.ToScalar();
            if (scalar != null && scalar.Type != DataType.String)
            {
                throw ScriptRuntimeException.IndexType(scalar);
            }

            return _inner.SetIndex(script, obj, index, value, isDirectIndexing);
        }

        public string AsString(object obj)
        {
            return _inner.AsString(obj);
        }

        public DynValue MetaIndex(Script script, object obj, string metaname)
        {
            return TryMetaIndex(script, obj, metaname, out DynValue value) ? value : null;
        }

        public bool TryMetaIndex(Script script, object obj, string metaname, out DynValue value)
        {
            return UserDataAccess.TryMetaIndex(_inner, script, obj, metaname, out value);
        }

        public bool IsTypeCompatible(Type type, object obj)
        {
            return _inner.IsTypeCompatible(type, obj);
        }
    }
}
