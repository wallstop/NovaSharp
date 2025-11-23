namespace NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors
{
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.Converters;

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
            int[] indices = BuildArrayIndices(args, args.Count - 1);
            DynValue value = args[^1];

            Type elemType = array.GetType().GetElementType();

            object objValue = ScriptToClrConversions.DynValueToObjectOfType(
                value,
                elemType,
                null,
                false
            );

            array.SetValue(objValue, indices);

            return DynValue.Void;
        }

        private static object ArrayIndexerGet(
            object arrayObj,
            ScriptExecutionContext ctx,
            CallbackArguments args
        )
        {
            Array array = (Array)arrayObj;
            int[] indices = BuildArrayIndices(args, args.Count);

            return array.GetValue(indices);
        }
    }
}
