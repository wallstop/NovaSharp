namespace WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Converters;

    /// <summary>
    /// Member descriptor for indexer of array types
    /// </summary>
    public class ArrayMemberDescriptor : ObjectCallbackMemberDescriptor, IWireableDescriptor
    {
        private readonly bool _isSetter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayMemberDescriptor"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="isSetter">if set to <c>true</c> is a setter indexer.</param>
        /// <param name="indexerParams">The indexer parameters.</param>
        public ArrayMemberDescriptor(
            string name,
            bool isSetter,
            ParameterDescriptor[] indexerParams
        )
            : base(
                name,
                isSetter
                    ? static (obj, ctx, args) => ArrayIndexerSet(obj, ctx, args)
                    : (Func<object, ScriptExecutionContext, CallbackArguments, object>)
                        ArrayIndexerGet,
                indexerParams
            )
        {
            _isSetter = isSetter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayMemberDescriptor"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="isSetter">if set to <c>true</c> [is setter].</param>
        public ArrayMemberDescriptor(string name, bool isSetter)
            : base(
                name,
                isSetter
                    ? static (obj, ctx, args) => ArrayIndexerSet(obj, ctx, args)
                    : (Func<object, ScriptExecutionContext, CallbackArguments, object>)
                        ArrayIndexerGet
            )
        {
            _isSetter = isSetter;
        }

        /// <summary>
        /// Prepares the descriptor for hard-wiring.
        /// The descriptor fills the passed table with all the needed data for hardwire generators to generate the appropriate code.
        /// </summary>
        /// <param name="t">The table to be filled</param>
        public void PrepareForWiring(Table t)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            t.Set("class", DynValue.NewString(GetType().FullName));
            t.Set("name", DynValue.NewString(Name));
            t.Set("setter", DynValue.NewBoolean(_isSetter));

            if (Parameters != null)
            {
                DynValue pars = DynValue.NewPrimeTable();

                t.Set("params", pars);

                int i = 0;

                foreach (ParameterDescriptor p in Parameters)
                {
                    DynValue pt = DynValue.NewPrimeTable();
                    pars.Table.Set(++i, pt);
                    p.PrepareForWiring(pt.Table);
                }
            }
        }

        private static int[] BuildArrayIndices(CallbackArguments args, int count)
        {
            int[] indices = new int[count];

            for (int i = 0; i < count; i++)
            {
                indices[i] = args.AsInt(i, "userdata_array_indexer");
            }

            return indices;
        }

        private static DynValue ArrayIndexerSet(
            object arrayObj,
            ScriptExecutionContext ctx,
            CallbackArguments args
        )
        {
            Array array = (Array)arrayObj;
            int indexCount = args.Count - 1;

            switch (indexCount)
            {
                case 1 when array.Rank == 1:
                {
                    int index0 = args.AsInt(0, "userdata_array_indexer");
                    DynValue value = args[^1];
                    object objValue = ConvertArrayValue(array, value);
                    array.SetValue(objValue, index0);
                    break;
                }
                case 2 when array.Rank == 2:
                {
                    int index0 = args.AsInt(0, "userdata_array_indexer");
                    int index1 = args.AsInt(1, "userdata_array_indexer");
                    DynValue value = args[^1];
                    object objValue = ConvertArrayValue(array, value);
                    array.SetValue(objValue, index0, index1);
                    break;
                }
                case 3 when array.Rank == 3:
                {
                    int index0 = args.AsInt(0, "userdata_array_indexer");
                    int index1 = args.AsInt(1, "userdata_array_indexer");
                    int index2 = args.AsInt(2, "userdata_array_indexer");
                    DynValue value = args[^1];
                    object objValue = ConvertArrayValue(array, value);
                    array.SetValue(objValue, index0, index1, index2);
                    break;
                }
                default:
                {
                    int[] indices = BuildArrayIndices(args, indexCount);
                    DynValue value = args[^1];
                    object objValue = ConvertArrayValue(array, value);
                    array.SetValue(objValue, indices);
                    break;
                }
            }

            return DynValue.Void;
        }

        private static object ConvertArrayValue(Array array, DynValue value)
        {
            Type elemType = array.GetType().GetElementType();

            return ScriptToClrConversions.DynValueToObjectOfType(value, elemType, null, false);
        }

        private static object ArrayIndexerGet(
            object arrayObj,
            ScriptExecutionContext ctx,
            CallbackArguments args
        )
        {
            Array array = (Array)arrayObj;

            switch (args.Count)
            {
                case 1 when array.Rank == 1:
                {
                    int index0 = args.AsInt(0, "userdata_array_indexer");
                    return array.GetValue(index0);
                }
                case 2 when array.Rank == 2:
                {
                    int index0 = args.AsInt(0, "userdata_array_indexer");
                    int index1 = args.AsInt(1, "userdata_array_indexer");
                    return array.GetValue(index0, index1);
                }
                case 3 when array.Rank == 3:
                {
                    int index0 = args.AsInt(0, "userdata_array_indexer");
                    int index1 = args.AsInt(1, "userdata_array_indexer");
                    int index2 = args.AsInt(2, "userdata_array_indexer");
                    return array.GetValue(index0, index1, index2);
                }
                default:
                    int[] indices = BuildArrayIndices(args, args.Count);
                    return array.GetValue(indices);
            }
        }
    }
}
