namespace WallstopStudios.NovaSharp.Interpreter.Interop
{
    using System.Runtime.CompilerServices;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Bridges legacy null-return userdata access to explicit presence-aware capabilities.
    /// </summary>
    internal static class UserDataAccess
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryIndex(
            IUserDataDescriptor descriptor,
            Script script,
            object obj,
            LuaValue index,
            bool isDirectIndexing,
            out LuaValue value
        )
        {
            if (descriptor.TryIndex(script, obj, index, isDirectIndexing, out value))
            {
                return true;
            }

            value = LuaValue.Nil;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryMetaIndex(
            IUserDataDescriptor descriptor,
            Script script,
            object obj,
            string metaname,
            out LuaValue value
        )
        {
            if (descriptor.TryMetaIndex(script, obj, metaname, out value))
            {
                return true;
            }

            value = LuaValue.Nil;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryIndex(
            IUserDataType userData,
            Script script,
            LuaValue index,
            bool isDirectIndexing,
            out LuaValue value
        )
        {
            if (userData.TryIndex(script, index, isDirectIndexing, out value))
            {
                return true;
            }

            value = LuaValue.Nil;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryMetaIndex(
            IUserDataType userData,
            Script script,
            string metaname,
            out LuaValue value
        )
        {
            if (userData.TryMetaIndex(script, metaname, out value))
            {
                return true;
            }

            value = LuaValue.Nil;
            return false;
        }
    }
}
