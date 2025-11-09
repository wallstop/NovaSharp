using System;
using System.Reflection;
using NovaSharp.Interpreter.Compatibility;
using NovaSharp.Interpreter.Interop.Converters;

namespace NovaSharp.Interpreter.Serialization
{
    public static class ObjectValueConverter
    {
        public static DynValue SerializeObjectToDynValue(
            Script script,
            object o,
            DynValue valueForNulls = null
        )
        {
            if (o == null)
            {
                return valueForNulls ?? DynValue.Nil;
            }

            DynValue v = ClrToScriptConversions.TryObjectToTrivialDynValue(script, o);

            if (v != null)
            {
                return v;
            }

            if (o is Enum)
            {
                return DynValue.NewNumber(
                    NumericConversions.TypeToDouble(Enum.GetUnderlyingType(o.GetType()), o)
                );
            }

            Table t = new(script);

            System.Collections.IEnumerable ienum = o as System.Collections.IEnumerable;

            if (ienum != null)
            {
                foreach (object obj in ienum)
                {
                    t.Append(SerializeObjectToDynValue(script, obj, valueForNulls));
                }
            }
            else
            {
                Type type = o.GetType();

                foreach (PropertyInfo pi in Framework.Do.GetProperties(type))
                {
                    MethodInfo getter = Framework.Do.GetGetMethod(pi);
                    bool isStatic = getter.IsStatic;
                    object obj = getter.Invoke(isStatic ? null : o, null); // convoluted workaround for --full-aot Mono execution

                    t.Set(pi.Name, SerializeObjectToDynValue(script, obj, valueForNulls));
                }
            }

            return DynValue.NewTable(t);
        }
    }
}
