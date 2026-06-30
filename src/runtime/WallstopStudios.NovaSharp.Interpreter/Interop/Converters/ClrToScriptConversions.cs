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
            if (obj == null)
            {
                return DynValue.Nil;
            }

            if (obj is DynValue value)
            {
                return value;
            }

            DynValue primitive = TryObjectToPrimitiveDynValue(obj);
            if (primitive != null)
            {
                return primitive;
            }

            if (obj is Table table)
            {
                return DynValue.NewTable(table);
            }

            return null;
        }

        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using "simple" logic.
        /// Does NOT throw on failure.
        /// </summary>
        internal static DynValue TryObjectToSimpleDynValue(Script script, object obj)
        {
            if (obj == null)
            {
                return DynValue.Nil;
            }

            if (obj is DynValue value)
            {
                return value;
            }

            Type type = obj.GetType();
            Func<Script, object, DynValue> converter =
                Script.GlobalOptions.CustomConverters.GetClrToScriptCustomConversion(type);
            if (converter != null)
            {
                DynValue v = converter(script, obj);
                if (v != null)
                {
                    return v;
                }
            }

            DynValue primitive = TryObjectToPrimitiveDynValue(obj);
            if (primitive != null)
            {
                return primitive;
            }

            if (obj is Closure closure)
            {
                return DynValue.FromClosure(closure);
            }

            if (obj is Table table)
            {
                return DynValue.NewTable(table);
            }

            if (obj is CallbackFunction function)
            {
                return DynValue.FromCallback(function);
            }

            if (obj is ScriptFunctionCallbackView argumentViewCallback)
            {
                return DynValue.NewCallbackView(argumentViewCallback);
            }

            if (obj is Delegate @delegate)
            {
#if NETFX_CORE
                MethodInfo mi = d.GetMethodInfo();
#else
                MethodInfo mi = @delegate.Method;
#endif

                if (CallbackFunction.CheckArgumentViewCallbackSignature(mi, false))
                {
                    return DynValue.NewCallbackView(
                        CreateDelegate<ScriptFunctionCallbackView>(@delegate, mi)
                    );
                }

                if (CallbackFunction.CheckLegacyCallbackSignature(mi, false))
                {
                    return DynValue.NewCallback(
                        CreateDelegate<Func<ScriptExecutionContext, CallbackArguments, DynValue>>(
                            @delegate,
                            mi
                        )
                    );
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to convert a CLR object to a NovaSharp value, using more in-depth analysis
        /// </summary>
        internal static DynValue ObjectToDynValue(Script script, object obj)
        {
            DynValue v = TryObjectToSimpleDynValue(script, obj);

            if (v != null)
            {
                return v;
            }

            v = UserData.Create(obj);
            if (v != null)
            {
                return v;
            }

            if (obj is Type type)
            {
                v = UserData.CreateStatic(type);
            }

            // unregistered enums go as integers
            if (obj is Enum)
            {
                return DynValue.NewNumber(
                    NumericConversions.TypeToDouble(Enum.GetUnderlyingType(obj.GetType()), obj)
                );
            }

            if (v != null)
            {
                return v;
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

            DynValue enumerator = EnumerationToDynValue(script, obj);
            if (enumerator != null)
            {
                return enumerator;
            }

            throw ScriptRuntimeException.ConvertObjectFailed(obj);
        }

        private static DynValue TryObjectToPrimitiveDynValue(object obj)
        {
            if (obj is bool boolValue)
            {
                return DynValue.FromBoolean(boolValue);
            }

            if (obj is string stringValue)
            {
                return DynValue.NewString(stringValue);
            }

            if (obj is StringBuilder || obj is char)
            {
                return DynValue.NewString(obj.ToString());
            }

            if (obj is double doubleValue)
            {
                return DynValue.FromNumber(doubleValue);
            }

            if (obj is decimal decimalValue)
            {
                return DynValue.FromNumber(Convert.ToDouble(decimalValue));
            }

            if (obj is float floatValue)
            {
                return DynValue.FromNumber(floatValue);
            }

            if (obj is long longValue)
            {
                return DynValue.FromInteger(longValue);
            }

            if (obj is int intValue)
            {
                return DynValue.FromInteger(intValue);
            }

            if (obj is short shortValue)
            {
                return DynValue.FromInteger(shortValue);
            }

            if (obj is sbyte sbyteValue)
            {
                return DynValue.FromInteger(sbyteValue);
            }

            if (obj is ulong ulongValue)
            {
                return DynValue.FromInteger(checked((long)ulongValue));
            }

            if (obj is uint uintValue)
            {
                return DynValue.FromInteger(uintValue);
            }

            if (obj is ushort ushortValue)
            {
                return DynValue.FromInteger(ushortValue);
            }

            if (obj is byte byteValue)
            {
                return DynValue.FromInteger(byteValue);
            }

            return null;
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
            if (obj is IEnumerable enumerable)
            {
                return EnumerableWrapper.ConvertIterator(script, enumerable.GetEnumerator());
            }

            if (obj is IEnumerator enumer)
            {
                return EnumerableWrapper.ConvertIterator(script, enumer);
            }

            return null;
        }
    }
}
