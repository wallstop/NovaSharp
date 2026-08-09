namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System.Runtime.CompilerServices;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop;

    /// <content>
    /// Provides script-facing helpers for metatable and script access.
    /// </content>
    internal sealed partial class Processor
    {
        /// <summary>
        /// Gets the metatable associated with the specified value, honoring type metatables when needed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// Attempts to resolve the metamethod invoked for a binary operation between
        /// <paramref name="op1"/> and <paramref name="op2"/>.
        /// </summary>
        internal bool TryGetBinaryMetamethod(
            DynValue op1,
            DynValue op2,
            string eventName,
            out DynValue metamethod
        )
        {
            Table op1MetaTable = GetMetatable(op1);
            if (op1MetaTable != null)
            {
                if (op1MetaTable.TryRawGet(eventName, out DynValue meta1) && meta1.IsNotNil())
                {
                    metamethod = meta1;
                    return true;
                }
            }

            Table op2MetaTable = GetMetatable(op2);
            if (op2MetaTable != null)
            {
                if (op2MetaTable.TryRawGet(eventName, out DynValue meta2) && meta2.IsNotNil())
                {
                    metamethod = meta2;
                    return true;
                }
            }

            if (op1.Type == DataType.UserData)
            {
                if (
                    UserDataAccess.TryMetaIndex(
                        op1.UserData.Descriptor,
                        _script,
                        op1.UserData.Object,
                        eventName,
                        out metamethod
                    )
                )
                {
                    return true;
                }
            }

            if (op2.Type == DataType.UserData)
            {
                if (
                    UserDataAccess.TryMetaIndex(
                        op2.UserData.Descriptor,
                        _script,
                        op2.UserData.Object,
                        eventName,
                        out metamethod
                    )
                )
                {
                    return true;
                }
            }

            metamethod = DynValue.Nil;
            return false;
        }

        /// <summary>
        /// Attempts to resolve the metamethod for the given value, probing userdata descriptors first.
        /// </summary>
        internal bool TryGetMetamethod(
            DynValue value,
            string metamethod,
            out DynValue resolvedMetamethod
        )
        {
            if (value.Type == DataType.UserData)
            {
                if (
                    UserDataAccess.TryMetaIndex(
                        value.UserData.Descriptor,
                        _script,
                        value.UserData.Object,
                        metamethod,
                        out resolvedMetamethod
                    )
                )
                {
                    return true;
                }
            }

            return TryGetMetamethodRaw(value, metamethod, out resolvedMetamethod);
        }

        /// <summary>
        /// Resolves the metamethod for the given value, or returns <see langword="null"/> when none is
        /// available.
        /// </summary>
        internal DynValue GetMetamethod(DynValue value, string metamethod)
        {
            return TryGetMetamethod(value, metamethod, out DynValue resolvedMetamethod)
                ? resolvedMetamethod
                : null;
        }

        /// <summary>
        /// Attempts to resolve the metamethod from the metatable only (no userdata descriptor lookup).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetMetamethodRaw(
            DynValue value,
            string metamethod,
            out DynValue resolvedMetamethod
        )
        {
            Table metatable = GetMetatable(value);

            if (metatable == null)
            {
                resolvedMetamethod = DynValue.Nil;
                return false;
            }

            if (
                !metatable.TryRawGet(metamethod, out resolvedMetamethod)
                || resolvedMetamethod.IsNil()
            )
            {
                resolvedMetamethod = DynValue.Nil;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolves the metamethod from the metatable only, or returns <see langword="null"/> when none
        /// is available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DynValue GetMetamethodRaw(DynValue value, string metamethod)
        {
            return TryGetMetamethodRaw(value, metamethod, out DynValue resolvedMetamethod)
                ? resolvedMetamethod
                : null;
        }

        /// <summary>
        /// Gets the owning script for this processor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Script GetScript()
        {
            return _script;
        }
    }
}
