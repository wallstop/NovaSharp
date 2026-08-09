// Disable warnings about XML documentation
namespace WallstopStudios.NovaSharp.Interpreter.LuaPort.LuaStateInterop
{
#pragma warning disable IDE1006 // Mirrors upstream Lua C API naming (snake_case preserved intentionally).

    using System.Collections.Generic;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    /// <summary>
    ///
    /// </summary>
    public class LuaState
    {
        private readonly List<LuaValue> _stack;

        public ScriptExecutionContext ExecutionContext { get; private set; }
        public string FunctionName { get; private set; }

        internal LuaState(
            ScriptExecutionContext executionContext,
            CallbackArguments args,
            string functionName
        )
        {
            ExecutionContext = executionContext;
            _stack = new List<LuaValue>(16);

            for (int i = 0; i < args.Count; i++)
            {
                _stack.Add(args[i]);
            }

            FunctionName = functionName;
        }

        public LuaValue Top(int pos = 0)
        {
            return _stack[_stack.Count - 1 - pos];
        }

        public LuaValue At(int pos)
        {
            if (pos < 0)
            {
                pos = _stack.Count + pos + 1;
            }

            if (pos > _stack.Count)
            {
                return LuaValue.Void;
            }

            return _stack[pos - 1];
        }

        public int Count
        {
            get { return _stack.Count; }
        }

        public void Push(LuaValue v)
        {
            _stack.Add(v);
        }

        public LuaValue Pop()
        {
            LuaValue v = Top();
            _stack.RemoveAt(_stack.Count - 1);
            return v;
        }

        public LuaValue[] GetTopArray(int num)
        {
            LuaValue[] rets = new LuaValue[num];

            for (int i = 0; i < num; i++)
            {
                rets[num - i - 1] = Top(i);
            }

            return rets;
        }

        public LuaValue GetReturnValue(int retvals)
        {
            if (retvals == 0)
            {
                return LuaValue.Nil;
            }
            else if (retvals == 1)
            {
                return Top();
            }
            else
            {
                LuaValue[] rets = GetTopArray(retvals);
                return LuaValue.NewTupleNested(rets);
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
