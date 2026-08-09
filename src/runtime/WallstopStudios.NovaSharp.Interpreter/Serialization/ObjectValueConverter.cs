namespace WallstopStudios.NovaSharp.Interpreter.Serialization
{
    using System;
    using System.Collections;
    using System.Reflection;
    using Interop.Converters;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Converts CLR objects (primitives, collections, POCOs) to <see cref="DynValue"/> trees.
    /// </summary>
    public static class ObjectValueConverter
    {
        /// <summary>
        /// Serializes a CLR object into a Lua value, recursively walking enumerables and properties.
        /// </summary>
        /// <param name="script">Owning script used for table allocation and conversions.</param>
        /// <param name="o">The CLR object to convert.</param>
        /// <returns>A <see cref="DynValue"/> representing the object graph.</returns>
        public static DynValue SerializeObjectToDynValue(Script script, object o)
        {
            return SerializeObjectToDynValueCore(script, o, DynValue.Nil);
        }

        /// <summary>
        /// Serializes a CLR object into a Lua value, recursively walking enumerables and properties.
        /// </summary>
        /// <param name="script">Owning script used for table allocation and conversions.</param>
        /// <param name="o">The CLR object to convert.</param>
        /// <param name="valueForNulls">The value used when encountering <c>null</c> references.</param>
        /// <returns>A <see cref="DynValue"/> representing the object graph.</returns>
        public static DynValue SerializeObjectToDynValue(
            Script script,
            object o,
            DynValue valueForNulls
        )
        {
            return SerializeObjectToDynValueCore(script, o, valueForNulls ?? DynValue.Nil);
        }

        private static DynValue SerializeObjectToDynValueCore(
            Script script,
            object o,
            DynValue valueForNulls
        )
        {
            if (o == null)
            {
                return valueForNulls;
            }

            if (ClrToScriptConversions.TryObjectToTrivialDynValue(script, o, out DynValue value))
            {
                return value;
            }

            if (o is Enum)
            {
                return DynValue.NewNumber(
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

            return DynValue.NewTable(t);
        }
    }
}
