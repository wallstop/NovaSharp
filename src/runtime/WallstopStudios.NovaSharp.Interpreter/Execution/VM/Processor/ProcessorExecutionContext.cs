namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System.Runtime.CompilerServices;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
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
        internal Table GetMetatable(LuaValue value)
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
            LuaValue op1,
            LuaValue op2,
            string eventName,
            out LuaValue metamethod
        )
        {
            if (
                TryGetBinaryMetatableMetamethod(op1, eventName, out metamethod)
                || TryGetBinaryMetatableMetamethod(op2, eventName, out metamethod)
            )
            {
                return true;
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

            metamethod = LuaValue.Nil;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetBinaryMetatableMetamethod(
            LuaValue value,
            string eventName,
            out LuaValue metamethod
        )
        {
            // Lua thread equality uses identity. A CLR userdata operand can still supply its
            // descriptor equality operation through TryGetBinaryMetamethod.
            if (value.Type == DataType.Thread && eventName == Metamethods.Eq)
            {
                metamethod = LuaValue.Nil;
                return false;
            }

            return TryGetMetamethodRaw(value, eventName, out metamethod);
        }

        /// <summary>
        /// Attempts to resolve the metamethod for the given value, probing userdata descriptors first.
        /// </summary>
        internal bool TryGetMetamethod(
            LuaValue value,
            string metamethod,
            out LuaValue resolvedMetamethod
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
        /// Resolves the metamethod for the given value, or returns <see cref="LuaValue.Nil"/> when
        /// none is available.
        /// </summary>
        internal LuaValue GetMetamethod(LuaValue value, string metamethod)
        {
            return TryGetMetamethod(value, metamethod, out LuaValue resolvedMetamethod)
                ? resolvedMetamethod
                : LuaValue.Nil;
        }

        /// <summary>
        /// Attempts to resolve the metamethod from the metatable only (no userdata descriptor lookup).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetMetamethodRaw(
            LuaValue value,
            string metamethod,
            out LuaValue resolvedMetamethod
        )
        {
            Table metatable = GetMetatable(value);

            if (metatable == null)
            {
                resolvedMetamethod = LuaValue.Nil;
                return false;
            }

            if (
                !metatable.TryRawGet(metamethod, out resolvedMetamethod) || resolvedMetamethod.IsNil
            )
            {
                resolvedMetamethod = LuaValue.Nil;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolves the metamethod from the metatable only, or returns <see cref="LuaValue.Nil"/>
        /// when none is available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LuaValue GetMetamethodRaw(LuaValue value, string metamethod)
        {
            return TryGetMetamethodRaw(value, metamethod, out LuaValue resolvedMetamethod)
                ? resolvedMetamethod
                : LuaValue.Nil;
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
