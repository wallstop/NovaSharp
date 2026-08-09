namespace WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors
{
    using System;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    /// <summary>
    /// Lightweight wrapper exposed to Lua scripts so CLR events surface <c>add</c>/<c>remove</c> helpers.
    /// </summary>
    internal class EventFacade : IUserDataTypeTryAccess
    {
        private readonly Func<
            object,
            ScriptExecutionContext,
            CallbackArguments,
            LuaValue
        > _addCallback;
        private readonly Func<
            object,
            ScriptExecutionContext,
            CallbackArguments,
            LuaValue
        > _removeCallback;
        private readonly object _object;

        /// <summary>
        /// Initializes a facade that uses the reflection-based descriptor callbacks.
        /// </summary>
        public EventFacade(EventMemberDescriptor parent, object obj)
        {
            _object = obj;
            _addCallback = parent.AddCallback;
            _removeCallback = parent.RemoveCallback;
        }

        /// <summary>
        /// Initializes a facade with explicit add/remove delegates (used by custom descriptors).
        /// </summary>
        public EventFacade(
            Func<object, ScriptExecutionContext, CallbackArguments, LuaValue> addCallback,
            Func<object, ScriptExecutionContext, CallbackArguments, LuaValue> removeCallback,
            object obj
        )
        {
            _object = obj;
            _addCallback = addCallback;
            _removeCallback = removeCallback;
        }

        /// <summary>
        /// Exposes <c>add</c> and <c>remove</c> members that wire into the underlying CLR event.
        /// </summary>
        public LuaValue? Index(Script script, LuaValue index, bool isDirectIndexing)
        {
            TryIndex(script, index, isDirectIndexing, out LuaValue value);
            return value;
        }

        /// <inheritdoc/>
        public bool TryIndex(
            Script script,
            LuaValue index,
            bool isDirectIndexing,
            out LuaValue value
        )
        {
            if (index.Type == DataType.String)
            {
                if (index.String == "add")
                {
                    value = LuaValue.NewCallback(script, (c, a) => _addCallback(_object, c, a));
                    return true;
                }
                else if (index.String == "remove")
                {
                    value = LuaValue.NewCallback(script, (c, a) => _removeCallback(_object, c, a));
                    return true;
                }
            }

            throw new ScriptRuntimeException("Events only support add and remove methods");
        }

        /// <summary>
        /// Events are read-only; any attempt to assign members throws.
        /// </summary>
        public bool SetIndex(Script script, LuaValue index, LuaValue value, bool isDirectIndexing)
        {
            throw new ScriptRuntimeException("Events do not have settable fields");
        }

        /// <summary>
        /// Event facades do not expose any metamethods.
        /// </summary>
        public LuaValue? MetaIndex(Script script, string metaname)
        {
            return TryMetaIndex(script, metaname, out LuaValue value) ? value : (LuaValue?)null;
        }

        /// <inheritdoc/>
        public bool TryMetaIndex(Script script, string metaname, out LuaValue value)
        {
            value = LuaValue.Nil;
            return false;
        }
    }
}
