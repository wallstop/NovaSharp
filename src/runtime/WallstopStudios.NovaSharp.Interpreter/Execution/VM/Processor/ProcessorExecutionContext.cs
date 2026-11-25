namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Runtime.CompilerServices;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib.IO;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
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

            return TryGetBinaryDescriptorMetamethod(op1, op2, eventName, out metamethod);
        }

        /// <summary>Resolves the ordering hook according to the script profile.</summary>
        internal bool TryGetOrderingMetamethod(
            LuaValue left,
            LuaValue right,
            string eventName,
            out LuaValue metamethod
        )
        {
            if (_script.CompatibilityVersion != LuaCompatibilityVersion.Lua51)
            {
                return TryGetBinaryMetamethod(left, right, eventName, out metamethod);
            }

            // Lua 5.1 requires matching Lua types and raw-equal ordering hooks.
            DataType leftType = GetOrderingType(left.Type);
            DataType rightType = GetOrderingType(right.Type);
            if (
                leftType == rightType
                && TryGetMetamethodRaw(
                    GetOrderingMetatable(left, leftType),
                    eventName,
                    out metamethod
                )
                && TryGetMetamethodRaw(
                    GetOrderingMetatable(right, rightType),
                    eventName,
                    out LuaValue rightMetamethod
                )
                && (
                    metamethod.Type == DataType.UserData
                        ? metamethod.HasSameReferenceIdentity(rightMetamethod)
                        : metamethod.Equals(rightMetamethod, LuaCompatibilityVersion.Lua51)
                )
            )
            {
                return true;
            }

            // CLR descriptors supply host operators independently of Lua type metatables.
            return TryGetBinaryDescriptorMetamethod(left, right, eventName, out metamethod);
        }

        private static DataType GetOrderingType(DataType type)
        {
            return type switch
            {
                DataType.Void => DataType.Nil,
                DataType.ClrFunction => DataType.Function,
                _ => type,
            };
        }

        private Table GetOrderingMetatable(LuaValue value, DataType type)
        {
            return value.Type == type ? GetMetatable(value) : _script.GetTypeMetatable(type);
        }

        private bool TryGetBinaryDescriptorMetamethod(
            LuaValue op1,
            LuaValue op2,
            string eventName,
            out LuaValue metamethod
        )
        {
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
            return TryGetMetamethodRaw(GetMetatable(value), metamethod, out resolvedMetamethod);
        }

        private static bool TryGetMetamethodRaw(
            Table metatable,
            string metamethod,
            out LuaValue resolvedMetamethod
        )
        {
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

        /// <summary>Creates an ordering diagnostic from the operands in comparison order.</summary>
        internal ScriptRuntimeException CreateOrderingError(LuaValue left, LuaValue right)
        {
            bool useMetatableNames =
                LuaVersionDefaults.Resolve(_script.CompatibilityVersion)
                >= LuaCompatibilityVersion.Lua53;
            return ScriptRuntimeException.CompareInvalidType(
                GetDiagnosticTypeName(left, useMetatableNames),
                GetDiagnosticTypeName(right, useMetatableNames)
            );
        }

        private static string GetDiagnosticTypeName(LuaValue value, bool useMetatableNames)
        {
            if (useMetatableNames)
            {
                if (
                    value.Type == DataType.Table
                    && value.Table.MetaTable is Table metatable
                    && metatable.TryRawGet(Metamethods.Name, out LuaValue name)
                    && name.Type == DataType.String
                )
                {
                    string label = name.String;
                    int terminator = label.IndexOf('\0', StringComparison.Ordinal);
                    return terminator < 0 ? label : label[..terminator];
                }

                // Standard Lua installs this name on its file userdata metatable.
                if (
                    value.Type == DataType.UserData
                    && value.UserData.Descriptor is FileUserDataDescriptor
                )
                {
                    return "FILE*";
                }
            }

            return value.Type.ToLuaTypeString();
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
