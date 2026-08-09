namespace WallstopStudios.NovaSharp.Interpreter.Interop.Converters
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Text;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.PredefinedUserData;

    /// <summary>
    /// Converts CLR objects into NovaSharp <see cref="LuaValue"/> instances.
    /// </summary>
    internal static class ClrToScriptConversions
    {
        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using "trivial" logic.
        /// Skips on custom conversions, etc.
        /// Does NOT throw on failure.
        /// </summary>
        internal static LuaValue? TryObjectToTrivialDynValue(Script script, object obj)
        {
            return TryObjectToTrivialDynValue(script, obj, out LuaValue result)
                ? result
                : (LuaValue?)null;
        }

        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using "trivial" logic.
        /// </summary>
        internal static bool TryObjectToTrivialDynValue(
            Script script,
            object obj,
            out LuaValue result
        )
        {
            if (obj == null)
            {
                result = LuaValue.Nil;
                return true;
            }

            if (obj is LuaValue value)
            {
                result = value;
                return true;
            }

            if (TryObjectToPrimitiveDynValue(obj, out result))
            {
                return true;
            }

            if (obj is Table table)
            {
                result = LuaValue.NewTable(table);
                return true;
            }

            result = LuaValue.Nil;
            return false;
        }

        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using "simple" logic.
        /// Does NOT throw on failure.
        /// </summary>
        internal static LuaValue? TryObjectToSimpleDynValue(Script script, object obj)
        {
            return TryObjectToSimpleDynValue(script, obj, out LuaValue result)
                ? result
                : (LuaValue?)null;
        }

        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using "simple" logic.
        /// </summary>
        internal static bool TryObjectToSimpleDynValue(
            Script script,
            object obj,
            out LuaValue result
        )
        {
            if (obj == null)
            {
                result = LuaValue.Nil;
                return true;
            }

            if (obj is LuaValue value)
            {
                result = value;
                return true;
            }

            Type type = obj.GetType();
            if (
                Script.GlobalOptions.CustomConverters.TryConvertClrToScript(
                    type,
                    script,
                    obj,
                    out result
                )
            )
            {
                return true;
            }

            if (TryObjectToPrimitiveDynValue(obj, out result))
            {
                return true;
            }

            if (obj is Closure closure)
            {
                result = LuaValue.FromClosure(closure);
                return true;
            }

            if (obj is Table table)
            {
                result = LuaValue.NewTable(table);
                return true;
            }

            if (obj is CallbackFunction function)
            {
                result = LuaValue.FromCallback(
                    script == null ? function : function.BindToScript(script)
                );
                return true;
            }

            if (obj is ScriptFunctionCallbackView argumentViewCallback)
            {
                result = LuaValue.NewCallbackView(script, argumentViewCallback);
                return true;
            }

            if (obj is ScriptFunctionCallbackViewNoContext argumentViewNoContextCallback)
            {
                result = LuaValue.NewCallbackView(script, argumentViewNoContextCallback);
                return true;
            }

            if (obj is Delegate @delegate)
            {
#if NETFX_CORE
                MethodInfo mi = @delegate.GetMethodInfo();
#else
                MethodInfo mi = @delegate.Method;
#endif

                if (CallbackFunction.CheckArgumentViewNoContextCallbackSignature(mi, false))
                {
                    result = LuaValue.NewCallbackView(
                        script,
                        CreateDelegate<ScriptFunctionCallbackViewNoContext>(@delegate, mi)
                    );
                    return true;
                }

                if (CallbackFunction.CheckArgumentViewCallbackSignature(mi, false))
                {
                    result = LuaValue.NewCallbackView(
                        script,
                        CreateDelegate<ScriptFunctionCallbackView>(@delegate, mi)
                    );
                    return true;
                }

                if (CallbackFunction.CheckLegacyCallbackSignature(mi, false))
                {
                    result = LuaValue.NewCallback(
                        script,
                        CreateDelegate<Func<ScriptExecutionContext, CallbackArguments, LuaValue>>(
                            @delegate,
                            mi
                        )
                    );
                    return true;
                }
            }

            result = LuaValue.Nil;
            return false;
        }

        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using more in-depth analysis
        /// </summary>
        internal static LuaValue ObjectToDynValue(Script script, object obj)
        {
            if (TryObjectToSimpleDynValue(script, obj, out LuaValue value))
            {
                return value;
            }

            if (UserData.TryCreate(script, obj, out value))
            {
                return value;
            }

            // unregistered enums go as integers
            if (obj is Enum)
            {
                return LuaValue.NewNumber(
                    NumericConversions.TypeToDouble(Enum.GetUnderlyingType(obj.GetType()), obj)
                );
            }

            if (obj is Delegate @delegate)
            {
                return LuaValue.NewCallback(CallbackFunction.FromDelegate(script, @delegate));
            }

            if (obj is MethodInfo mi)
            {
                if (mi.IsStatic)
                {
                    return LuaValue.NewCallback(CallbackFunction.FromMethodInfo(script, mi));
                }
            }

            if (obj is IList list)
            {
                Table t = TableConversions.ConvertIListToTable(script, list);
                return LuaValue.NewTable(t);
            }

            if (obj is IDictionary dictionary)
            {
                Table t = TableConversions.ConvertIDictionaryToTable(script, dictionary);
                return LuaValue.NewTable(t);
            }

            if (TryEnumerationToDynValue(script, obj, out LuaValue enumerator))
            {
                return enumerator;
            }

            throw ScriptRuntimeException.ConvertObjectFailed(obj);
        }

        private static bool TryObjectToPrimitiveDynValue(object obj, out LuaValue result)
        {
            if (obj is bool boolValue)
            {
                result = LuaValue.FromBoolean(boolValue);
                return true;
            }

            if (obj is string stringValue)
            {
                result = LuaValue.NewString(stringValue);
                return true;
            }

            if (obj is StringBuilder || obj is char)
            {
                result = LuaValue.NewString(obj.ToString());
                return true;
            }

            if (obj is double doubleValue)
            {
                result = LuaValue.FromNumber(doubleValue);
                return true;
            }

            if (obj is decimal decimalValue)
            {
                result = LuaValue.FromNumber(Convert.ToDouble(decimalValue));
                return true;
            }

            if (obj is float floatValue)
            {
                result = LuaValue.FromNumber(floatValue);
                return true;
            }

            if (obj is long longValue)
            {
                result = LuaValue.FromInteger(longValue);
                return true;
            }

            if (obj is int intValue)
            {
                result = LuaValue.FromInteger(intValue);
                return true;
            }

            if (obj is short shortValue)
            {
                result = LuaValue.FromInteger(shortValue);
                return true;
            }

            if (obj is sbyte sbyteValue)
            {
                result = LuaValue.FromInteger(sbyteValue);
                return true;
            }

            if (obj is ulong ulongValue)
            {
                result = LuaValue.FromInteger(checked((long)ulongValue));
                return true;
            }

            if (obj is uint uintValue)
            {
                result = LuaValue.FromInteger(uintValue);
                return true;
            }

            if (obj is ushort ushortValue)
            {
                result = LuaValue.FromInteger(ushortValue);
                return true;
            }

            if (obj is byte byteValue)
            {
                result = LuaValue.FromInteger(byteValue);
                return true;
            }

            result = LuaValue.Nil;
            return false;
        }

        private static TDelegate CreateDelegate<TDelegate>(Delegate source, MethodInfo mi)
            where TDelegate : Delegate
        {
            if (source is TDelegate typed)
            {
                return typed;
            }

            return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), source.Target, mi);
        }

        /// <summary>
        /// Converts an IEnumerable or IEnumerator to a LuaValue
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static LuaValue? EnumerationToDynValue(Script script, object obj)
        {
            return TryEnumerationToDynValue(script, obj, out LuaValue result)
                ? result
                : (LuaValue?)null;
        }

        /// <summary>
        /// Attempts to convert an <see cref="IEnumerable"/> or <see cref="IEnumerator"/> to a
        /// script iterator tuple.
        /// </summary>
        public static bool TryEnumerationToDynValue(Script script, object obj, out LuaValue result)
        {
            if (obj is IEnumerable enumerable)
            {
                result = EnumerableWrapper.ConvertIterator(script, enumerable.GetEnumerator());
                return true;
            }

            if (obj is IEnumerator enumer)
            {
                result = EnumerableWrapper.ConvertIterator(script, enumer);
                return true;
            }

            result = LuaValue.Nil;
            return false;
        }
    }
}
