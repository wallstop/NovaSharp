namespace WallstopStudios.NovaSharp.Interpreter.Interop
{
    using System;
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
            if (descriptor is IUserDataDescriptorTryAccess tryAccess)
            {
                if (tryAccess.TryIndex(script, obj, index, isDirectIndexing, out value))
                {
                    EnsureHandledValue(value);
                    return true;
                }

                value = DynValue.Nil;
                return false;
            }

            value = descriptor.Index(script, obj, index, isDirectIndexing);
            if (value != null)
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
            if (descriptor is IUserDataDescriptorTryAccess tryAccess)
            {
                if (tryAccess.TryMetaIndex(script, obj, metaname, out value))
                {
                    EnsureHandledValue(value);
                    return true;
                }

                value = DynValue.Nil;
                return false;
            }

            value = descriptor.MetaIndex(script, obj, metaname);
            if (value != null)
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
            if (userData is IUserDataTypeTryAccess tryAccess)
            {
                if (tryAccess.TryIndex(script, index, isDirectIndexing, out value))
                {
                    EnsureHandledValue(value);
                    return true;
                }

                value = DynValue.Nil;
                return false;
            }

            value = userData.Index(script, index, isDirectIndexing);
            if (value != null)
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
            if (userData is IUserDataTypeTryAccess tryAccess)
            {
                if (tryAccess.TryMetaIndex(script, metaname, out value))
                {
                    EnsureHandledValue(value);
                    return true;
                }

                value = DynValue.Nil;
                return false;
            }

            value = userData.MetaIndex(script, metaname);
            if (value != null)
            {
                return true;
            }

            value = DynValue.Nil;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureHandledValue(DynValue value)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    "A userdata Try access provider returned true with a null value."
                );
            }
        }
    }
}
