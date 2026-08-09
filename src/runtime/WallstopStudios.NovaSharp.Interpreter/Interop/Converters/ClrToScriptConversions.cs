namespace WallstopStudios.NovaSharp.Interpreter.Interop.Converters
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.PredefinedUserData;

    /// <summary>
    /// Converts CLR objects into NovaSharp <see cref="DynValue"/> instances.
    /// </summary>
    internal static class ClrToScriptConversions
    {
        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using "trivial" logic.
        /// Skips on custom conversions, etc.
        /// Does NOT throw on failure.
        /// </summary>
        internal static DynValue TryObjectToTrivialDynValue(Script script, object obj)
        {
            return TryObjectToTrivialDynValue(script, obj, out DynValue result) ? result : null;
        }

        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using "trivial" logic.
        /// </summary>
        internal static bool TryObjectToTrivialDynValue(
            Script script,
            object obj,
            out DynValue result
        )
        {
            if (obj == null)
            {
                result = DynValue.Nil;
                return true;
            }

            if (obj is DynValue value)
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
                result = DynValue.NewTable(table);
                return true;
            }

            result = DynValue.Nil;
            return false;
        }

        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using "simple" logic.
        /// Does NOT throw on failure.
        /// </summary>
        internal static DynValue TryObjectToSimpleDynValue(Script script, object obj)
        {
            return TryObjectToSimpleDynValue(script, obj, out DynValue result) ? result : null;
        }

        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using "simple" logic.
        /// </summary>
        internal static bool TryObjectToSimpleDynValue(
            Script script,
            object obj,
            out DynValue result
        )
        {
            if (obj == null)
            {
                result = DynValue.Nil;
                return true;
            }

            if (obj is DynValue value)
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
                result = DynValue.FromClosure(closure);
                return true;
            }

            if (obj is Table table)
            {
                result = DynValue.NewTable(table);
                return true;
            }

            if (obj is CallbackFunction function)
            {
                result = DynValue.FromCallback(function);
                return true;
            }

            if (obj is ScriptFunctionCallbackView argumentViewCallback)
            {
                result = DynValue.NewCallbackView(argumentViewCallback);
                return true;
            }

            if (obj is ScriptFunctionCallbackViewNoContext argumentViewNoContextCallback)
            {
                result = DynValue.NewCallbackView(argumentViewNoContextCallback);
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
                    result = DynValue.NewCallbackView(
                        CreateDelegate<ScriptFunctionCallbackViewNoContext>(@delegate, mi)
                    );
                    return true;
                }

                if (CallbackFunction.CheckArgumentViewCallbackSignature(mi, false))
                {
                    result = DynValue.NewCallbackView(
                        CreateDelegate<ScriptFunctionCallbackView>(@delegate, mi)
                    );
                    return true;
                }

                if (CallbackFunction.CheckLegacyCallbackSignature(mi, false))
                {
                    result = DynValue.NewCallback(
                        CreateDelegate<Func<ScriptExecutionContext, CallbackArguments, DynValue>>(
                            @delegate,
                            mi
                        )
                    );
                    return true;
                }
            }

            result = DynValue.Nil;
            return false;
        }

        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using more in-depth analysis
        /// </summary>
        internal static DynValue ObjectToDynValue(Script script, object obj)
        {
            if (TryObjectToSimpleDynValue(script, obj, out DynValue value))
            {
                return value;
            }

            if (UserData.TryCreate(obj, out value))
            {
                return value;
            }

            // unregistered enums go as integers
            if (obj is Enum)
            {
                return DynValue.NewNumber(
                    NumericConversions.TypeToDouble(Enum.GetUnderlyingType(obj.GetType()), obj)
                );
            }

            if (obj is Delegate @delegate)
            {
                return DynValue.NewCallback(CallbackFunction.FromDelegate(script, @delegate));
            }

            if (obj is MethodInfo mi)
            {
                if (mi.IsStatic)
                {
                    return DynValue.NewCallback(CallbackFunction.FromMethodInfo(script, mi));
                }
            }

            if (obj is IList list)
            {
                Table t = TableConversions.ConvertIListToTable(script, list);
                return DynValue.NewTable(t);
            }

            if (obj is IDictionary dictionary)
            {
                Table t = TableConversions.ConvertIDictionaryToTable(script, dictionary);
                return DynValue.NewTable(t);
            }

            if (TryEnumerationToDynValue(script, obj, out DynValue enumerator))
            {
                return enumerator;
            }

            throw ScriptRuntimeException.ConvertObjectFailed(obj);
        }

        private static bool TryObjectToPrimitiveDynValue(object obj, out DynValue result)
        {
            if (obj is bool boolValue)
            {
                result = DynValue.FromBoolean(boolValue);
                return true;
            }

            if (obj is string stringValue)
            {
                result = DynValue.NewString(stringValue);
                return true;
            }

            if (obj is StringBuilder || obj is char)
            {
                result = DynValue.NewString(obj.ToString());
                return true;
            }

            if (obj is double doubleValue)
            {
                result = DynValue.FromNumber(doubleValue);
                return true;
            }

            if (obj is decimal decimalValue)
            {
                result = DynValue.FromNumber(Convert.ToDouble(decimalValue));
                return true;
            }

            if (obj is float floatValue)
            {
                result = DynValue.FromNumber(floatValue);
                return true;
            }

            if (obj is long longValue)
            {
                result = DynValue.FromInteger(longValue);
                return true;
            }

            if (obj is int intValue)
            {
                result = DynValue.FromInteger(intValue);
                return true;
            }

            if (obj is short shortValue)
            {
                result = DynValue.FromInteger(shortValue);
                return true;
            }

            if (obj is sbyte sbyteValue)
            {
                result = DynValue.FromInteger(sbyteValue);
                return true;
            }

            if (obj is ulong ulongValue)
            {
                result = DynValue.FromInteger(checked((long)ulongValue));
                return true;
            }

            if (obj is uint uintValue)
            {
                result = DynValue.FromInteger(uintValue);
                return true;
            }

            if (obj is ushort ushortValue)
            {
                result = DynValue.FromInteger(ushortValue);
                return true;
            }

            if (obj is byte byteValue)
            {
                result = DynValue.FromInteger(byteValue);
                return true;
            }

            result = DynValue.Nil;
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
        /// Converts an IEnumerable or IEnumerator to a DynValue
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static DynValue EnumerationToDynValue(Script script, object obj)
        {
            return TryEnumerationToDynValue(script, obj, out DynValue result) ? result : null;
        }

        /// <summary>
        /// Attempts to convert an <see cref="IEnumerable"/> or <see cref="IEnumerator"/> to a
        /// script iterator tuple.
        /// </summary>
        public static bool TryEnumerationToDynValue(Script script, object obj, out DynValue result)
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

            result = DynValue.Nil;
            return false;
        }
    }
}
