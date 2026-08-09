namespace WallstopStudios.NovaSharp.Interpreter.Interop
{
    using System.Runtime.CompilerServices;
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
            DynValue index,
            bool isDirectIndexing,
            out DynValue value
        )
        {
            if (descriptor.TryIndex(script, obj, index, isDirectIndexing, out value))
            {
                return true;
            }

            value = DynValue.Nil;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryMetaIndex(
            IUserDataDescriptor descriptor,
            Script script,
            object obj,
            string metaname,
            out DynValue value
        )
        {
            if (descriptor.TryMetaIndex(script, obj, metaname, out value))
            {
                return true;
            }

            value = DynValue.Nil;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryIndex(
            IUserDataType userData,
            Script script,
            DynValue index,
            bool isDirectIndexing,
            out DynValue value
        )
        {
            if (userData.TryIndex(script, index, isDirectIndexing, out value))
            {
                return true;
            }

            value = DynValue.Nil;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryMetaIndex(
            IUserDataType userData,
            Script script,
            string metaname,
            out DynValue value
        )
        {
            if (userData.TryMetaIndex(script, metaname, out value))
            {
                return true;
            }

            value = DynValue.Nil;
            return false;
        }
    }
}
