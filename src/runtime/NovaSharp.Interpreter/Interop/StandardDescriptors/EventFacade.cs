namespace NovaSharp.Interpreter.Interop.StandardDescriptors
{
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    internal class EventFacade : IUserDataType
    {
        private readonly Func<
            object,
            ScriptExecutionContext,
            CallbackArguments,
            DynValue
        > _addCallback;
        private readonly Func<
            object,
            ScriptExecutionContext,
            CallbackArguments,
            DynValue
        > _removeCallback;
        private readonly object _object;

        public EventFacade(EventMemberDescriptor parent, object obj)
        {
            _object = obj;
            _addCallback = parent.AddCallback;
            _removeCallback = parent.RemoveCallback;
        }

        public EventFacade(
            Func<object, ScriptExecutionContext, CallbackArguments, DynValue> addCallback,
            Func<object, ScriptExecutionContext, CallbackArguments, DynValue> removeCallback,
            object obj
        )
        {
            _object = obj;
            _addCallback = addCallback;
            _removeCallback = removeCallback;
        }

        public DynValue Index(Script script, DynValue index, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                if (index.String == "add")
                {
                    return DynValue.NewCallback((c, a) => _addCallback(_object, c, a));
                }
                else if (index.String == "remove")
                {
                    return DynValue.NewCallback((c, a) => _removeCallback(_object, c, a));
                }
            }

            throw new ScriptRuntimeException("Events only support add and remove methods");
        }

        public bool SetIndex(Script script, DynValue index, DynValue value, bool isDirectIndexing)
        {
            throw new ScriptRuntimeException("Events do not have settable fields");
        }

        public DynValue MetaIndex(Script script, string metaname)
        {
            return null;
        }
    }
}
