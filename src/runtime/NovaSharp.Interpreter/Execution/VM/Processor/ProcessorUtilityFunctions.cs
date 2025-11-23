namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataTypes;

    /// <content>
    /// Provides shared helpers for tuple adjustment, metamethod invocation, and stack inspection.
    /// </content>
    internal sealed partial class Processor
    {
        /// <summary>
        /// Normalizes a list of return values so trailing tuples are expanded per Lua rules.
        /// </summary>
        private static DynValue[] InternalAdjustTuple(IList<DynValue> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<DynValue>();
            }

            if (values[^1].Type == DataType.Tuple)
            {
                int baseLen = values.Count - 1 + values[^1].Tuple.Length;
                DynValue[] result = new DynValue[baseLen];

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
                DynValue[] result = new DynValue[values.Count];

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
            DynValue op1,
            string eventName,
            int instructionPtr
        )
        {
            DynValue m = null;

            if (op1.Type == DataType.UserData)
            {
                m = op1.UserData.Descriptor.MetaIndex(_script, op1.UserData.Object, eventName);
            }

            if (m == null)
            {
                Table op1MetaTable = GetMetatable(op1);

                if (op1MetaTable != null)
                {
                    DynValue meta1 = op1MetaTable.RawGet(eventName);
                    if (meta1 != null && meta1.IsNotNil())
                    {
                        m = meta1;
                    }
                }
            }

            if (m != null)
            {
                _valueStack.Push(m);
                _valueStack.Push(op1);
                return InternalExecCall(1, instructionPtr);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Pushes a binary metamethod on the stack and schedules its execution.
        /// </summary>
        private int InternalInvokeBinaryMetaMethod(
            DynValue l,
            DynValue r,
            string eventName,
            int instructionPtr,
            DynValue extraPush = null
        )
        {
            DynValue m = GetBinaryMetamethod(l, r, eventName);

            if (m != null)
            {
                if (extraPush != null)
                {
                    _valueStack.Push(extraPush);
                }

                _valueStack.Push(m);
                _valueStack.Push(l);
                _valueStack.Push(r);
                return InternalExecCall(2, instructionPtr);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Copies or pops the top <paramref name="items"/> entries from the value stack.
        /// </summary>
        private DynValue[] StackTopToArray(int items, bool pop)
        {
            DynValue[] values = new DynValue[items];

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
        /// Copies or pops the top <paramref name="items"/> entries from the value stack in reverse order.
        /// </summary>
        private DynValue[] StackTopToArrayReverse(int items, bool pop)
        {
            DynValue[] values = new DynValue[items];

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
    }
}
