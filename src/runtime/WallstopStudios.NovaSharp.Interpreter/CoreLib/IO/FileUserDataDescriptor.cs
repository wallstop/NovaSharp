namespace WallstopStudios.NovaSharp.Interpreter.CoreLib.IO
{
    using System;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
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

        public LuaValue? Index(Script script, object obj, LuaValue index, bool isDirectIndexing)
        {
            return TryIndex(script, obj, index, isDirectIndexing, out LuaValue value)
                ? value
                : (LuaValue?)null;
        }

        public bool TryIndex(
            Script script,
            object obj,
            LuaValue index,
            bool isDirectIndexing,
            out LuaValue value
        )
        {
            LuaValue scalar = index.ToScalar();
            if (scalar.Type != DataType.String)
            {
                value = LuaValue.Nil;
                return true;
            }

            if (
                obj is FileUserDataBase file
                && TryCreateArgumentViewCallback(script, file, scalar.String, out value)
            )
            {
                return true;
            }

            return UserDataAccess.TryIndex(_inner, script, obj, index, isDirectIndexing, out value);
        }

        private static bool TryCreateArgumentViewCallback(
            Script script,
            FileUserDataBase file,
            string name,
            out LuaValue value
        )
        {
            ScriptFunctionCallbackView callback = name switch
            {
                "close" => Close,
                "flush" => Flush,
                "lines" => Lines,
                "read" => Read,
                "seek" => Seek,
                "setvbuf" => SetBuffer,
                "write" => Write,
                _ => null,
            };
            if (callback == null)
            {
                value = LuaValue.Nil;
                return false;
            }

            CallbackFunction function = CallbackFunction.FromArgumentView(script, callback, name);
            function.AdditionalData = file;
            value = LuaValue.NewCallback(function);
            return true;
        }

        private static LuaValue Close(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return GetFile(executionContext).Close(executionContext, args.SkipMethodCall());
        }

        private static LuaValue Flush(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return LuaValue.NewBoolean(GetFile(executionContext).Flush());
        }

        private static LuaValue Lines(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return GetFile(executionContext).Lines(executionContext, args.SkipMethodCall());
        }

        private static LuaValue Read(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return GetFile(executionContext).Read(executionContext, args.SkipMethodCall());
        }

        private static LuaValue Seek(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            CallbackArgumentsView normalized = args.SkipMethodCall();
            LuaValue whenceValue = normalized[0];
            string whence = whenceValue.ToObject<string>();
            LuaValue offsetValue = normalized[1];
            long offset = offsetValue.Type is DataType.Void or DataType.Nil
                ? 0L
                : offsetValue.ToObject<long>();
            return LuaValue.NewNumber(GetFile(executionContext).Seek(whence, offset));
        }

        private static LuaValue SetBuffer(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            CallbackArgumentsView normalized = args.SkipMethodCall();
            string mode = normalized[0].ToObject<string>();
            return LuaValue.NewBoolean(GetFile(executionContext).Setvbuf(mode));
        }

        private static LuaValue Write(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            return GetFile(executionContext).Write(executionContext, args.SkipMethodCall());
        }

        private static FileUserDataBase GetFile(ScriptExecutionContext executionContext)
        {
            if (executionContext.AdditionalData is not FileUserDataBase file)
            {
                throw new InvalidOperationException("File callback target is unavailable.");
            }

            return file;
        }

        public bool SetIndex(
            Script script,
            object obj,
            LuaValue index,
            LuaValue value,
            bool isDirectIndexing
        )
        {
            LuaValue scalar = index.ToScalar();
            if (scalar.Type != DataType.String)
            {
                throw ScriptRuntimeException.IndexType(scalar);
            }

            return _inner.SetIndex(script, obj, index, value, isDirectIndexing);
        }

        public string AsString(object obj)
        {
            return _inner.AsString(obj);
        }

        public LuaValue? MetaIndex(Script script, object obj, string metaname)
        {
            return TryMetaIndex(script, obj, metaname, out LuaValue value)
                ? value
                : (LuaValue?)null;
        }

        public bool TryMetaIndex(Script script, object obj, string metaname, out LuaValue value)
        {
            return UserDataAccess.TryMetaIndex(_inner, script, obj, metaname, out value);
        }

        public bool IsTypeCompatible(Type type, object obj)
        {
            return _inner.IsTypeCompatible(type, obj);
        }
    }
}
