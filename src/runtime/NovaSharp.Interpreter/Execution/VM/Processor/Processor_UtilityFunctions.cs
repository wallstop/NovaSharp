namespace NovaSharp.Interpreter.Execution.VM
{
    using System.Collections.Generic;

    internal sealed partial class Processor
    {
        private DynValue[] Internal_AdjustTuple(IList<DynValue> values)
        {
            if (values == null || values.Count == 0)
            {
                return new DynValue[0];
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
                    return Internal_AdjustTuple(result);
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

        private int Internal_InvokeUnaryMetaMethod(
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
                return Internal_ExecCall(1, instructionPtr);
            }
            else
            {
                return -1;
            }
        }

        private int Internal_InvokeBinaryMetaMethod(
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
                return Internal_ExecCall(2, instructionPtr);
            }
            else
            {
                return -1;
            }
        }

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
