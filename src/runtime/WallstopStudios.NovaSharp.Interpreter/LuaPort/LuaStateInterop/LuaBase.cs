// Disable warnings about XML documentation
namespace WallstopStudios.NovaSharp.Interpreter.LuaPort.LuaStateInterop
{
#pragma warning disable IDE1006 // Mirrors upstream Lua C API naming (snake_case preserved intentionally).

    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using lua_Integer = System.Int32;

    /// <summary>
    /// Classes using the classic interface should inherit from this class.
    /// This class defines only static methods and is really meant to be used only
    /// from C# and not other .NET languages.
    ///
    /// For easier operation they should also define:
    ///		using ptrdiff_t = System.Int32;
    ///		using lua_Integer = System.Int32;
    ///		using LUA_INTFRM_T = System.Int64;
    ///		using UNSIGNED_LUA_INTFRM_T = System.UInt64;
    /// </summary>
    public static partial class LuaBase
    {
        internal const int LuaTypeNone = -1;
        internal const int LuaTypeNil = 0;
        internal const int LuaTypeBoolean = 1;
        internal const int LuaTypeLightUserData = 2;
        internal const int LuaTypeNumber = 3;
        internal const int LuaTypeString = 4;
        internal const int LuaTypeTable = 5;
        internal const int LuaTypeFunction = 6;
        internal const int LuaTypeUserData = 7;
        internal const int LuaTypeThread = 8;

        internal const int LuaMultipleResults = -1;

        internal const string LuaIntegerFormatLength = "l";

        internal static DynValue GetArgument(LuaState l, lua_Integer pos)
        {
            EnsureState(l, nameof(l));
            return l.At(pos);
        }

        internal static DynValue ArgAsType(
            LuaState l,
            lua_Integer pos,
            DataType type,
            bool allowNil = false
        )
        {
            EnsureState(l, nameof(l));
            return GetArgument(l, pos)
                .CheckType(
                    l.FunctionName,
                    type,
                    pos - 1,
                    allowNil
                        ? TypeValidationOptions.AllowNil | TypeValidationOptions.AutoConvert
                        : TypeValidationOptions.AutoConvert
                );
        }

        internal static lua_Integer LuaType(LuaState l, lua_Integer p)
        {
            switch (GetArgument(l, p).Type)
            {
                case DataType.Void:
                    return LuaTypeNone;
                case DataType.Nil:
                    return LuaTypeNil;
                case DataType.Boolean:
                    return LuaTypeNil;
                case DataType.Number:
                    return LuaTypeNumber;
                case DataType.String:
                    return LuaTypeString;
                case DataType.Function:
                    return LuaTypeFunction;
                case DataType.Table:
                    return LuaTypeTable;
                case DataType.UserData:
                    return LuaTypeUserData;
                case DataType.Thread:
                    return LuaTypeThread;
                case DataType.ClrFunction:
                    return LuaTypeFunction;
                case DataType.TailCallRequest:
                case DataType.YieldRequest:
                case DataType.Tuple:
                default:
                    throw new ScriptRuntimeException("Can't call LuaType on any type");
            }
        }

        internal static string LuaLCheckLString(LuaState l, lua_Integer argNum, out uint length)
        {
            string str = ArgAsType(l, argNum, DataType.String, false).String;
            length = (uint)str.Length;
            return str;
        }

        internal static void LuaPushInteger(LuaState l, lua_Integer val)
        {
            EnsureState(l, nameof(l));
            l.Push(DynValue.NewNumber(val));
        }

        internal static lua_Integer LuaToBoolean(LuaState l, lua_Integer p)
        {
            return GetArgument(l, p).CastToBool() ? 1 : 0;
        }

        internal static string LuaToLString(LuaState luaState, lua_Integer p, out uint l)
        {
            return LuaLCheckLString(luaState, p, out l);
        }

        internal static string LuaToString(LuaState luaState, lua_Integer p)
        {
            uint l;
            return LuaLCheckLString(luaState, p, out l);
        }

        internal static void LuaLAddValue(LuaLBuffer b)
        {
            EnsureBuffer(b, nameof(b));
            b.StringBuilder.Append(b.LuaState.Pop().ToPrintString());
        }

        internal static void LuaLAddLString(LuaLBuffer b, CharPtr s, uint p)
        {
            EnsureBuffer(b, nameof(b));
            EnsurePointer(s, nameof(s));
            b.StringBuilder.Append(s.ToString((int)p));
        }

        internal static void LuaLAddString(LuaLBuffer b, string s)
        {
            EnsureBuffer(b, nameof(b));
            EnsureStringNotNull(s, nameof(s));
            b.StringBuilder.Append(s);
        }

        internal static lua_Integer LuaLOptInteger(LuaState l, lua_Integer pos, lua_Integer def)
        {
            DynValue v = ArgAsType(l, pos, DataType.Number, true);

            if (v.IsNil())
            {
                return def;
            }
            else
            {
                return (int)v.Number;
            }
        }

        internal static lua_Integer LuaLCheckInteger(LuaState l, lua_Integer pos)
        {
            DynValue v = ArgAsType(l, pos, DataType.Number, false);
            return (int)v.Number;
        }

        internal static void LuaLArgCheck(
            LuaState l,
            bool condition,
            lua_Integer argNum,
            string message
        )
        {
            if (!condition)
            {
                LuaLArgError(l, argNum, message);
            }
        }

        internal static lua_Integer LuaLCheckInt(LuaState l, lua_Integer argNum)
        {
            return LuaLCheckInteger(l, argNum);
        }

        internal static lua_Integer LuaGetTop(LuaState l)
        {
            EnsureState(l, nameof(l));
            return l.Count;
        }

        internal static lua_Integer LuaLError(
            LuaState luaState,
            string message,
            params object[] args
        )
        {
            throw new ScriptRuntimeException(message, args);
        }

        internal static void LuaLAddChar(LuaLBuffer b, char p)
        {
            EnsureBuffer(b, nameof(b));
            b.StringBuilder.Append(p);
        }

        internal static void LuaLBuffInit(LuaState l, LuaLBuffer b) { }

        internal static void LuaPushLiteral(LuaState l, string literalString)
        {
            EnsureState(l, nameof(l));
            EnsureStringNotNull(literalString, nameof(literalString));
            l.Push(DynValue.NewString(literalString));
        }

        internal static void LuaLPushResult(LuaLBuffer b)
        {
            EnsureBuffer(b, nameof(b));
            LuaPushLiteral(b.LuaState, b.StringBuilder.ToString());
        }

        internal static void LuaPushLString(LuaState l, CharPtr s, uint len)
        {
            EnsureState(l, nameof(l));
            EnsurePointer(s, nameof(s));
            string ss = s.ToString((int)len);
            l.Push(DynValue.NewString(ss));
        }

        internal static void LuaLCheckStack(LuaState l, lua_Integer n, string message)
        {
            // nop ?
        }

        internal static string LuaQuoteLiteral(string p)
        {
            return "'" + p + "'";
        }

        internal static void LuaPushNil(LuaState l)
        {
            EnsureState(l, nameof(l));
            l.Push(DynValue.Nil);
        }

        internal static void LuaAssert(bool p)
        {
            // ??!
            // A lot of KopiLua methods fall here in valid state!

            //if (!p)
            //	throw new InternalErrorException("LuaAssert failed!");
        }

        internal static string LuaLTypeName(LuaState l, lua_Integer p)
        {
            EnsureState(l, nameof(l));
            return l.At(p).Type.ToErrorTypeString();
        }

        internal static lua_Integer LuaIsString(LuaState l, lua_Integer p)
        {
            EnsureState(l, nameof(l));
            DynValue v = l.At(p);
            return (v.Type == DataType.String || v.Type == DataType.Number) ? 1 : 0;
        }

        internal static void LuaPop(LuaState l, lua_Integer p)
        {
            EnsureState(l, nameof(l));
            for (int i = 0; i < p; i++)
            {
                l.Pop();
            }
        }

        internal static void LuaGetTable(LuaState l, lua_Integer p)
        {
            EnsureState(l, nameof(l));
            // DEBT: this should call metamethods, now it performs raw access
            DynValue key = l.Pop();
            DynValue table = l.At(p);

            if (table.Type != DataType.Table)
            {
                throw new NotImplementedException();
            }

            DynValue v = table.Table.Get(key);
            l.Push(v);
        }

        internal static int LuaLOptInt(LuaState l, lua_Integer pos, lua_Integer def)
        {
            return LuaLOptInteger(l, pos, def);
        }

        internal static CharPtr LuaLCheckString(LuaState l, lua_Integer p)
        {
            uint dummy;
            return LuaLCheckLString(l, p, out dummy);
        }

        internal static string LuaLCheckStringStr(LuaState l, lua_Integer p)
        {
            uint dummy;
            return LuaLCheckLString(l, p, out dummy);
        }

        internal static void LuaLArgError(LuaState l, lua_Integer arg, string p)
        {
            EnsureState(l, nameof(l));
            throw ScriptRuntimeException.BadArgument(arg - 1, l.FunctionName, p);
        }

        internal static double LuaLCheckNumber(LuaState l, lua_Integer pos)
        {
            DynValue v = ArgAsType(l, pos, DataType.Number, false);
            return v.Number;
        }

        /// <summary>
        /// Checks that the argument at position <paramref name="pos"/> is a number and returns
        /// its <see cref="LuaNumber"/> value, preserving the integer/float subtype.
        /// </summary>
        /// <remarks>
        /// This is critical for <c>string.format</c> with <c>%d</c>, <c>%i</c>, <c>%o</c>, <c>%x</c>, <c>%X</c>
        /// specifiers where integer precision must be preserved (e.g., <c>math.maxinteger</c>).
        /// </remarks>
        internal static LuaNumber LuaLCheckLuaNumber(LuaState l, lua_Integer pos)
        {
            DynValue v = ArgAsType(l, pos, DataType.Number, false);
            return v.LuaNumber;
        }

        internal static void LuaPushValue(LuaState l, lua_Integer arg)
        {
            EnsureState(l, nameof(l));
            DynValue v = l.At(arg);
            l.Push(v);
        }

        /// <summary>
        /// Calls a function.
        /// To call a function you must use the following protocol: first, the function to be called is pushed onto the stack; then,
        /// the arguments to the function are pushed in direct order; that is, the first argument is pushed first. Finally you call
        /// lua_call; nargs is the number of arguments that you pushed onto the stack. All arguments and the function value are
        /// popped from the stack when the function is called. The function results are pushed onto the stack when the function
        /// returns. The number of results is adjusted to nresults, unless nresults is LuaMultipleResults. In this case, all results from
        /// the function are pushed. Lua takes care that the returned values fit into the stack space. The function results are
        /// pushed onto the stack in direct order (the first result is pushed first), so that after the call the last result is on
        /// the top of the stack.
        /// </summary>
        /// <param name="l">The LuaState</param>
        /// <param name="nargs">The number of arguments.</param>
        /// <param name="nresults">The number of expected results.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static void LuaCall(
            LuaState l,
            lua_Integer nargs,
            lua_Integer nresults = LuaMultipleResults
        )
        {
            EnsureState(l, nameof(l));
            DynValue[] args = l.GetTopArray(nargs);

            l.Discard(nargs);

            DynValue func = l.Pop();

            DynValue ret = l.ExecutionContext.Call(func, args);

            if (nresults != 0)
            {
                if (nresults == -1)
                {
                    nresults = (ret.Type == DataType.Tuple) ? ret.Tuple.Length : 1;
                }

                DynValue[] vals =
                    (ret.Type == DataType.Tuple) ? ret.Tuple : new DynValue[1] { ret };

                int copied = 0;

                for (int i = 0; i < vals.Length && copied < nresults; i++, copied++)
                {
                    l.Push(vals[i]);
                }

                while (copied < nresults)
                {
                    l.Push(DynValue.Nil);
                    copied++;
                }
            }
        }

        private static void EnsureState(LuaState state, string paramName)
        {
            if (state == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        private static void EnsureBuffer(LuaLBuffer buffer, string paramName)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        private static void EnsurePointer(CharPtr ptr, string paramName)
        {
            if (ptr == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        private static void EnsureStringNotNull(string value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}
