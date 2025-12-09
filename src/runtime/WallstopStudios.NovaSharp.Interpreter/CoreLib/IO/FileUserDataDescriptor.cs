namespace WallstopStudios.NovaSharp.Interpreter.CoreLib.IO
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop;

    /// <summary>
    /// Wraps the default file userdata descriptor to align Lua semantics for numeric indexing.
    /// </summary>
    internal sealed class FileUserDataDescriptor : IUserDataDescriptor
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
            DynValue scalar = index?.ToScalar();
            if (scalar != null && scalar.Type != DataType.String)
            {
                return DynValue.Nil;
            }

            return _inner.Index(script, obj, index, isDirectIndexing);
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
            return _inner.MetaIndex(script, obj, metaname);
        }

        public bool IsTypeCompatible(Type type, object obj)
        {
            return _inner.IsTypeCompatible(type, obj);
        }
    }
}
