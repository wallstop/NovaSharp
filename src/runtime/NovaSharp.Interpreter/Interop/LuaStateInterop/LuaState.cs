// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.Interop.LuaStateInterop
{
#pragma warning disable 1591

    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;

    /// <summary>
    ///
    /// </summary>
    public class LuaState
    {
        private readonly List<DynValue> _stack;

        public ScriptExecutionContext ExecutionContext { get; private set; }
        public string FunctionName { get; private set; }

        internal LuaState(
            ScriptExecutionContext executionContext,
            CallbackArguments args,
            string functionName
        )
        {
            ExecutionContext = executionContext;
            _stack = new List<DynValue>(16);

            for (int i = 0; i < args.Count; i++)
            {
                _stack.Add(args[i]);
            }

            FunctionName = functionName;
        }

        public DynValue Top(int pos = 0)
        {
            return _stack[_stack.Count - 1 - pos];
        }

        public DynValue At(int pos)
        {
            if (pos < 0)
            {
                pos = _stack.Count + pos + 1;
            }

            if (pos > _stack.Count)
            {
                return DynValue.Void;
            }

            return _stack[pos - 1];
        }

        public int Count
        {
            get { return _stack.Count; }
        }

        public void Push(DynValue v)
        {
            _stack.Add(v);
        }

        public DynValue Pop()
        {
            DynValue v = Top();
            _stack.RemoveAt(_stack.Count - 1);
            return v;
        }

        public DynValue[] GetTopArray(int num)
        {
            DynValue[] rets = new DynValue[num];

            for (int i = 0; i < num; i++)
            {
                rets[num - i - 1] = Top(i);
            }

            return rets;
        }

        public DynValue GetReturnValue(int retvals)
        {
            if (retvals == 0)
            {
                return DynValue.Nil;
            }
            else if (retvals == 1)
            {
                return Top();
            }
            else
            {
                DynValue[] rets = GetTopArray(retvals);
                return DynValue.NewTupleNested(rets);
            }
        }

        public void Discard(int nargs)
        {
            for (int i = 0; i < nargs; i++)
            {
                _stack.RemoveAt(_stack.Count - 1);
            }
        }
    }
}
