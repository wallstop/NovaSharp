namespace NovaSharp.Interpreter.Interop.Converters
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Text;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.PredefinedUserData;

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

            Type t = obj.GetType();

            if (obj is bool b)
            {
                return DynValue.NewBoolean(b);
            }

            if (obj is string || obj is StringBuilder || obj is char)
            {
                return DynValue.NewString(obj.ToString());
            }

            if (NumericConversions.NumericTypes.Contains(t))
            {
                return DynValue.NewNumber(NumericConversions.TypeToDouble(t, obj));
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

            Func<Script, object, DynValue> converter =
                Script.GlobalOptions.CustomConverters.GetClrToScriptCustomConversion(obj.GetType());
            if (converter != null)
            {
                DynValue v = converter(script, obj);
                if (v != null)
                {
                    return v;
                }
            }

            Type t = obj.GetType();

            if (obj is bool b)
            {
                return DynValue.NewBoolean(b);
            }

            if (obj is string || obj is StringBuilder || obj is char)
            {
                return DynValue.NewString(obj.ToString());
            }

            if (obj is Closure closure)
            {
                return DynValue.NewClosure(closure);
            }

            if (NumericConversions.NumericTypes.Contains(t))
            {
                return DynValue.NewNumber(NumericConversions.TypeToDouble(t, obj));
            }

            if (obj is Table table)
            {
                return DynValue.NewTable(table);
            }

            if (obj is CallbackFunction function)
            {
                return DynValue.NewCallback(function);
            }

            if (obj is Delegate @delegate)
            {
#if NETFX_CORE
                MethodInfo mi = d.GetMethodInfo();
#else
                MethodInfo mi = @delegate.Method;
#endif

                if (CallbackFunction.CheckCallbackSignature(mi, false))
                {
                    return DynValue.NewCallback(
                        (Func<ScriptExecutionContext, CallbackArguments, DynValue>)@delegate
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
