namespace WallstopStudios.NovaSharp.Interpreter.Serialization
{
    using System;
    using System.Collections;
    using System.Reflection;
    using global::NovaSharp;
    using Interop.Converters;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Converts CLR objects (primitives, collections, POCOs) to <see cref="LuaValue"/> trees.
    /// </summary>
    public static class ObjectValueConverter
    {
        /// <summary>
        /// Serializes a CLR object into a Lua value, recursively walking enumerables and properties.
        /// </summary>
        /// <param name="script">Owning script used for table allocation and conversions.</param>
        /// <param name="o">The CLR object to convert.</param>
        /// <returns>A <see cref="LuaValue"/> representing the object graph.</returns>
        public static LuaValue SerializeObjectToDynValue(Script script, object o)
        {
            return SerializeObjectToDynValueCore(script, o, LuaValue.Nil);
        }

        /// <summary>
        /// Serializes a CLR object into a Lua value, recursively walking enumerables and properties.
        /// </summary>
        /// <param name="script">Owning script used for table allocation and conversions.</param>
        /// <param name="o">The CLR object to convert.</param>
        /// <param name="valueForNulls">The value used when encountering <c>null</c> references.</param>
        /// <returns>A <see cref="LuaValue"/> representing the object graph.</returns>
        public static LuaValue SerializeObjectToDynValue(
            Script script,
            object o,
            LuaValue? valueForNulls
        )
        {
            return SerializeObjectToDynValueCore(script, o, valueForNulls.GetValueOrDefault());
        }

        private static LuaValue SerializeObjectToDynValueCore(
            Script script,
            object o,
            LuaValue valueForNulls
        )
        {
            if (o == null)
            {
                return valueForNulls;
            }

            if (ClrToScriptConversions.TryObjectToTrivialDynValue(script, o, out LuaValue value))
            {
                return value;
            }

            if (o is Enum)
            {
                return LuaValue.NewNumber(
                    NumericConversions.TypeToDouble(Enum.GetUnderlyingType(o.GetType()), o)
                );
            }

            Table t = new(script);

            if (o is IEnumerable ienum)
            {
                foreach (object obj in ienum)
                {
                    t.Append(SerializeObjectToDynValueCore(script, obj, valueForNulls));
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

                    t.Set(pi.Name, SerializeObjectToDynValueCore(script, obj, valueForNulls));
                }
            }

            return LuaValue.NewTable(t);
        }
    }
}
