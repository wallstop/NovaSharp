namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <content>
    /// Provides shared helpers for tuple adjustment, metamethod invocation, and stack inspection.
    /// </content>
    internal sealed partial class Processor
    {
        /// <summary>
        /// Normalizes a list of return values so trailing tuples are expanded per Lua rules.
        /// </summary>
        private static LuaValue[] InternalAdjustTuple(IList<LuaValue> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<LuaValue>();
            }

            if (values[^1].Type == DataType.Tuple)
            {
                int baseLen = values.Count - 1 + values[^1].Tuple.Length;
                LuaValue[] result = new LuaValue[baseLen];

                for (int i = 0; i < values.Count - 1; i++)
                {
                    result[i] = values[i].ToScalar();
                }

                for (int i = 0; i < values[^1].Tuple.Length; i++)
                {
                    result[values.Count + i - 1] = values[^1].Tuple[i];
                }

                if (result[^1].Type == DataType.Tuple)
                {
                    return InternalAdjustTuple(result);
                }
                else
                {
                    return result;
                }
            }
            else
            {
                LuaValue[] result = new LuaValue[values.Count];

                for (int i = 0; i < values.Count; i++)
                {
                    result[i] = values[i].ToScalar();
                }

                return result;
            }
        }

        /// <summary>
        /// Pushes a unary metamethod on the stack and schedules its execution.
        /// </summary>
        private int InternalInvokeUnaryMetaMethod(
            LuaValue op1,
            string eventName,
            int instructionPtr
        )
        {
            if (TryGetMetamethod(op1, eventName, out LuaValue metamethod))
            {
                _valueStack.Push(metamethod);
                _valueStack.Push(op1);
                return InternalExecCall(1, instructionPtr);
            }

            return -1;
        }

        /// <summary>
        /// Pushes a binary metamethod on the stack and schedules its execution.
        /// </summary>
        private int InternalInvokeBinaryMetaMethod(
            LuaValue l,
            LuaValue r,
            string eventName,
            int instructionPtr
        )
        {
            return InternalInvokeBinaryMetaMethodCore(
                l,
                r,
                eventName,
                instructionPtr,
                hasExtraPush: false,
                LuaValue.Nil
            );
        }

        /// <summary>
        /// Pushes an additional explicit Lua value before scheduling a binary metamethod.
        /// </summary>
        private int InternalInvokeBinaryMetaMethod(
            LuaValue l,
            LuaValue r,
            string eventName,
            int instructionPtr,
            LuaValue extraPush
        )
        {
            return InternalInvokeBinaryMetaMethodCore(
                l,
                r,
                eventName,
                instructionPtr,
                hasExtraPush: true,
                extraPush
            );
        }

        private int InternalInvokeBinaryMetaMethodCore(
            LuaValue l,
            LuaValue r,
            string eventName,
            int instructionPtr,
            bool hasExtraPush,
            LuaValue extraPush
        )
        {
            if (TryGetBinaryMetamethod(l, r, eventName, out LuaValue metamethod))
            {
                if (hasExtraPush)
                {
                    _valueStack.Push(extraPush);
                }

                _valueStack.Push(metamethod);
                _valueStack.Push(l);
                _valueStack.Push(r);
                return InternalExecCall(2, instructionPtr);
            }

            return -1;
        }

        /// <summary>
        /// Copies or pops the top <paramref name="items"/> entries from the value stack.
        /// </summary>
        private LuaValue[] StackTopToArray(int items, bool pop)
        {
            LuaValue[] values = DynValueArrayPool.Rent(items);

            if (pop)
            {
                for (int i = 0; i < items; i++)
                {
                    values[i] = _valueStack.Pop();
                }
            }
            else
            {
                for (int i = 0; i < items; i++)
                {
                    values[i] = _valueStack[_valueStack.Count - 1 - i];
                }
            }

            return values;
        }

        /// <summary>
        /// Copies or pops the top <paramref name="items"/> entries from the value stack,
        /// returning a pooled array that must be returned via <see cref="DynValueArrayPool.Return"/>.
        /// </summary>
        /// <param name="items">Number of items to copy/pop.</param>
        /// <param name="pop">If true, pops items from stack; otherwise copies without removing.</param>
        /// <param name="values">The pooled array containing the values.</param>
        /// <returns>A pooled resource that automatically returns the array when disposed.</returns>
        private PooledResource<LuaValue[]> StackTopToArrayPooled(
            int items,
            bool pop,
            out LuaValue[] values
        )
        {
            PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(items, out values);

            if (pop)
            {
                for (int i = 0; i < items; i++)
                {
                    values[i] = _valueStack.Pop();
                }
            }
            else
            {
                for (int i = 0; i < items; i++)
                {
                    values[i] = _valueStack[_valueStack.Count - 1 - i];
                }
            }

            return pooled;
        }

        /// <summary>
        /// Copies or pops the top <paramref name="items"/> entries from the value stack in reverse order.
        /// </summary>
        private LuaValue[] StackTopToArrayReverse(int items, bool pop)
        {
            LuaValue[] values = DynValueArrayPool.Rent(items);

            if (pop)
            {
                for (int i = 0; i < items; i++)
                {
                    values[items - 1 - i] = _valueStack.Pop();
                }
            }
            else
            {
                for (int i = 0; i < items; i++)
                {
                    values[items - 1 - i] = _valueStack[_valueStack.Count - 1 - i];
                }
            }

            return values;
        }

        /// <summary>
        /// Copies or pops the top <paramref name="items"/> entries from the value stack in reverse order,
        /// returning a pooled array that must be returned via <see cref="DynValueArrayPool.Return"/>.
        /// </summary>
        /// <param name="items">Number of items to copy/pop.</param>
        /// <param name="pop">If true, pops items from stack; otherwise copies without removing.</param>
        /// <param name="values">The pooled array containing the values.</param>
        /// <returns>A pooled resource that automatically returns the array when disposed.</returns>
        private PooledResource<LuaValue[]> StackTopToArrayReversePooled(
            int items,
            bool pop,
            out LuaValue[] values
        )
        {
            PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(items, out values);

            if (pop)
            {
                for (int i = 0; i < items; i++)
                {
                    values[items - 1 - i] = _valueStack.Pop();
                }
            }
            else
            {
                for (int i = 0; i < items; i++)
                {
                    values[items - 1 - i] = _valueStack[_valueStack.Count - 1 - i];
                }
            }

            return pooled;
        }
    }
}
