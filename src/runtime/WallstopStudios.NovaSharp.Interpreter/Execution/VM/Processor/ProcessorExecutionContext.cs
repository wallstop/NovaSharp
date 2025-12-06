namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <content>
    /// Provides script-facing helpers for metatable and script access.
    /// </content>
    internal sealed partial class Processor
    {
        /// <summary>
        /// Gets the metatable associated with the specified value, honoring type metatables when needed.
        /// </summary>
        internal Table GetMetatable(DynValue value)
        {
            if (value.Type == DataType.Table)
            {
                return value.Table.MetaTable;
            }
            else if (value.Type.CanHaveTypeMetatables())
            {
                return _script.GetTypeMetatable(value.Type);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Resolves the metamethod invoked for a binary operation between <paramref name="op1"/> and <paramref name="op2"/>.
        /// </summary>
        internal DynValue GetBinaryMetamethod(DynValue op1, DynValue op2, string eventName)
        {
            Table op1MetaTable = GetMetatable(op1);
            if (op1MetaTable != null)
            {
                DynValue meta1 = op1MetaTable.RawGet(eventName);
                if (meta1 != null && meta1.IsNotNil())
                {
                    return meta1;
                }
            }

            Table op2MetaTable = GetMetatable(op2);
            if (op2MetaTable != null)
            {
                DynValue meta2 = op2MetaTable.RawGet(eventName);
                if (meta2 != null && meta2.IsNotNil())
                {
                    return meta2;
                }
            }

            if (op1.Type == DataType.UserData)
            {
                DynValue meta = op1.UserData.Descriptor.MetaIndex(
                    _script,
                    op1.UserData.Object,
                    eventName
                );

                if (meta != null)
                {
                    return meta;
                }
            }

            if (op2.Type == DataType.UserData)
            {
                DynValue meta = op2.UserData.Descriptor.MetaIndex(
                    _script,
                    op2.UserData.Object,
                    eventName
                );

                if (meta != null)
                {
                    return meta;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves the metamethod for the given value, probing userdata descriptors first.
        /// </summary>
        internal DynValue GetMetamethod(DynValue value, string metamethod)
        {
            if (value.Type == DataType.UserData)
            {
                DynValue v = value.UserData.Descriptor.MetaIndex(
                    _script,
                    value.UserData.Object,
                    metamethod
                );
                if (v != null)
                {
                    return v;
                }
            }

            return GetMetamethodRaw(value, metamethod);
        }

        /// <summary>
        /// Resolves the metamethod from the metatable only (no userdata descriptor lookup).
        /// </summary>
        internal DynValue GetMetamethodRaw(DynValue value, string metamethod)
        {
            Table metatable = GetMetatable(value);

            if (metatable == null)
            {
                return null;
            }

            DynValue metameth = metatable.RawGet(metamethod);

            if (metameth == null || metameth.IsNil())
            {
                return null;
            }

            return metameth;
        }

        /// <summary>
        /// Gets the owning script for this processor.
        /// </summary>
        internal Script GetScript()
        {
            return _script;
        }
    }
}
