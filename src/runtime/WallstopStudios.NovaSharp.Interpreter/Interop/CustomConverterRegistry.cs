namespace WallstopStudios.NovaSharp.Interpreter.Interop
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Attempts to convert a CLR object to a script value.
    /// </summary>
    /// <param name="script">The script owning the converted value.</param>
    /// <param name="value">The CLR value to convert.</param>
    /// <param name="result">The converted value when the conversion is handled.</param>
    /// <returns><see langword="true"/> when the converter handled the value; otherwise, <see langword="false"/>.</returns>
    public delegate bool ClrToScriptTryConverter(Script script, object value, out DynValue result);

    /// <summary>
    /// Attempts to convert a strongly typed CLR object to a script value.
    /// </summary>
    /// <typeparam name="T">The CLR type accepted by the converter.</typeparam>
    /// <param name="script">The script owning the converted value.</param>
    /// <param name="value">The CLR value to convert.</param>
    /// <param name="result">The converted value when the conversion is handled.</param>
    /// <returns><see langword="true"/> when the converter handled the value; otherwise, <see langword="false"/>.</returns>
    public delegate bool ClrToScriptTryConverter<T>(Script script, T value, out DynValue result);

    /// <summary>
    /// A collection of custom converters between NovaSharp types and CLR types.
    /// If a converter function is not specified, returns null, or declines a try-conversion, the
    /// standard conversion path applies.
    /// </summary>
    public class CustomConverterRegistry
    {
        private readonly Dictionary<Type, Func<DynValue, object>>[] _script2Clr = new Dictionary<
            Type,
            Func<DynValue, object>
        >[(int)LuaTypeExtensions.MaxConvertibleTypes + 1];
        private readonly Dictionary<Type, ClrToScriptConverterEntry> _clr2Script = new();

        private readonly struct ClrToScriptConverterEntry
        {
            internal ClrToScriptConverterEntry(
                Func<Script, object, DynValue?> converter,
                ClrToScriptTryConverter tryConverter
            )
            {
                Converter = converter;
                TryConverter = tryConverter;
            }

            internal Func<Script, object, DynValue?> Converter { get; }

            internal ClrToScriptTryConverter TryConverter { get; }
        }

        internal CustomConverterRegistry()
        {
            for (int i = 0; i < _script2Clr.Length; i++)
            {
                _script2Clr[i] = new Dictionary<Type, Func<DynValue, object>>();
            }
        }

        // This needs to be evaluated further (doesn't work well with inheritance)
        //
        // 		private Dictionary<Type, Dictionary<Type, Func<object, object>>> _Script2ClrUserData = new Dictionary<Type, Dictionary<Type, Func<object, object>>>();
        //
        //public void SetScriptToClrUserDataSpecificCustomConversion(Type destType, Type userDataType, Func<object, object> converter = null)
        //{
        //	var destTypeMap = _Script2ClrUserData.GetOrCreate(destType, () => new Dictionary<Type, Func<object, object>>());
        //	destTypeMap[userDataType] = converter;

        //	SetScriptToClrCustomConversion(DataType.UserData, destType, v => DispatchUserDataCustomConverter(destTypeMap, v));
        //}

        //private object DispatchUserDataCustomConverter(Dictionary<Type, Func<object, object>> destTypeMap, DynValue v)
        //{
        //	if (v.Type != DataType.UserData)
        //		return null;

        //	if (v.UserData.Object == null)
        //		return null;

        //	Func<object, object> converter;

        //	for (Type userDataType = v.UserData.Object.GetType();
        //		userDataType != typeof(object);
        //		userDataType = userDataType.BaseType)
        //	{
        //		if (destTypeMap.TryGetValue(userDataType, out converter))
        //		{
        //			return converter(v.UserData.Object);
        //		}
        //	}

        //	return null;
        //}

        //public Func<object, object> GetScriptToClrUserDataSpecificCustomConversion(Type destType, Type userDataType)
        //{
        //	Dictionary<Type, Func<object, object>> destTypeMap;

        //	if (_Script2ClrUserData.TryGetValue(destType, out destTypeMap))
        //	{
        //		Func<object, object> converter;

        //		if (destTypeMap.TryGetValue(userDataType, out converter))
        //		{
        //			return converter;
        //		}
        //	}

        //	return null;
        //}

        /// <summary>
        /// Sets a custom converter from a script data type to a CLR data type. Set null to remove a previous custom converter.
        /// </summary>
        /// <param name="scriptDataType">The script data type</param>
        /// <param name="clrDataType">The CLR data type.</param>
        /// <param name="converter">The converter, or null.</param>
        public void SetScriptToClrCustomConversion(
            DataType scriptDataType,
            Type clrDataType,
            Func<DynValue, object> converter = null
        )
        {
            if ((int)scriptDataType >= _script2Clr.Length)
            {
                throw new ArgumentException(
                    "Script data type exceeds the registered converter range.",
                    nameof(scriptDataType)
                );
            }

            Dictionary<Type, Func<DynValue, object>> map = _script2Clr[(int)scriptDataType];

            if (converter == null)
            {
                map.Remove(clrDataType);
            }
            else
            {
                map[clrDataType] = converter;
            }
        }

        /// <summary>
        /// Gets a custom converter from a script data type to a CLR data type, or null
        /// </summary>
        /// <param name="scriptDataType">The script data type</param>
        /// <param name="clrDataType">The CLR data type.</param>
        /// <returns>The converter function, or null if not found</returns>
        public Func<DynValue, object> GetScriptToClrCustomConversion(
            DataType scriptDataType,
            Type clrDataType
        )
        {
            if ((int)scriptDataType >= _script2Clr.Length)
            {
                return null;
            }

            Dictionary<Type, Func<DynValue, object>> map = _script2Clr[(int)scriptDataType];
            return map.GetOrDefault(clrDataType);
        }

        /// <summary>
        /// Sets a custom converter from a CLR data type. Set null to remove a previous custom converter.
        /// </summary>
        /// <param name="clrDataType">The CLR data type.</param>
        /// <param name="converter">The converter, or null.</param>
        public void SetClrToScriptCustomConversion(
            Type clrDataType,
            Func<Script, object, DynValue?> converter = null
        )
        {
            if (converter == null)
            {
                _clr2Script.Remove(clrDataType);
            }
            else
            {
                ClrToScriptTryConverter tryConverter = (
                    Script script,
                    object value,
                    out DynValue result
                ) =>
                {
                    DynValue? converted = converter(script, value);
                    if (!converted.HasValue)
                    {
                        result = DynValue.Nil;
                        return false;
                    }

                    result = converted.Value;
                    return true;
                };
                _clr2Script[clrDataType] = new ClrToScriptConverterEntry(converter, tryConverter);
            }
        }

        /// <summary>
        /// Sets a custom converter from a CLR data type. Set null to remove a previous custom converter.
        /// </summary>
        /// <typeparam name="T">The CLR data type.</typeparam>
        /// <param name="converter">The converter, or null.</param>
        public void SetClrToScriptCustomConversion<T>(Func<Script, T, DynValue?> converter = null)
        {
            if (converter == null)
            {
                SetClrToScriptCustomConversion(typeof(T), (Func<Script, object, DynValue?>)null);
                return;
            }

            SetClrToScriptCustomConversion(typeof(T), (s, o) => converter(s, (T)o));
        }

        /// <summary>
        /// Gets a custom converter from a CLR data type, or null
        /// </summary>
        /// <param name="clrDataType">Type of the color data.</param>
        /// <returns>The converter function, or null if not found</returns>
        public Func<Script, object, DynValue?> GetClrToScriptCustomConversion(Type clrDataType)
        {
            return _clr2Script.TryGetValue(clrDataType, out ClrToScriptConverterEntry entry)
                ? entry.Converter
                : null;
        }

        /// <summary>
        /// Sets a custom try-converter from a CLR data type. Set null to remove a previous custom
        /// converter. Returning false declines the conversion and normalizes the output to
        /// <see cref="DynValue.Nil"/>. Returning true preserves explicit nil, void, and
        /// default-initialized nil results.
        /// </summary>
        /// <param name="clrDataType">The CLR data type.</param>
        /// <param name="converter">The try-converter, or null.</param>
        public void SetClrToScriptTryConversion(
            Type clrDataType,
            ClrToScriptTryConverter converter = null
        )
        {
            if (converter == null)
            {
                _clr2Script.Remove(clrDataType);
                return;
            }

            ClrToScriptTryConverter normalizedConverter = (
                Script script,
                object value,
                out DynValue result
            ) => NormalizeTryConversion(converter, script, value, out result);
            Func<Script, object, DynValue?> legacyConverter = (script, value) =>
                normalizedConverter(script, value, out DynValue result) ? result : null;
            _clr2Script[clrDataType] = new ClrToScriptConverterEntry(
                legacyConverter,
                normalizedConverter
            );
        }

        /// <summary>
        /// Sets a strongly typed custom try-converter from a CLR data type. Set null to remove a
        /// previous custom converter.
        /// </summary>
        /// <typeparam name="T">The CLR data type.</typeparam>
        /// <param name="converter">The try-converter, or null.</param>
        public void SetClrToScriptTryConversion<T>(ClrToScriptTryConverter<T> converter = null)
        {
            if (converter == null)
            {
                SetClrToScriptTryConversion(typeof(T), null);
                return;
            }

            SetClrToScriptTryConversion(
                typeof(T),
                (Script script, object value, out DynValue result) =>
                    converter(script, (T)value, out result)
            );
        }

        /// <summary>
        /// Gets a normalized custom try-converter from a CLR data type, or null when none is
        /// registered. Legacy converters are adapted so a null result declines conversion.
        /// </summary>
        /// <param name="clrDataType">The CLR data type.</param>
        /// <returns>The normalized try-converter, or null if not found.</returns>
        public ClrToScriptTryConverter GetClrToScriptTryConversion(Type clrDataType)
        {
            return _clr2Script.TryGetValue(clrDataType, out ClrToScriptConverterEntry entry)
                ? entry.TryConverter
                : null;
        }

        /// <summary>
        /// Attempts to convert a CLR value using its registered custom converter.
        /// </summary>
        internal bool TryConvertClrToScript(
            Type clrDataType,
            Script script,
            object value,
            out DynValue result
        )
        {
            if (_clr2Script.TryGetValue(clrDataType, out ClrToScriptConverterEntry entry))
            {
                return entry.TryConverter(script, value, out result);
            }

            result = DynValue.Nil;
            return false;
        }

        /// Sets a custom converter from a CLR data type. Set null to remove a previous custom converter.
        /// </summary>
        /// <param name="clrDataType">The CLR data type.</param>
        /// <param name="converter">The converter, or null.</param>
        [Obsolete(
            "This method is deprecated. Use the overloads accepting functions with a Script argument."
        )]
        public void SetClrToScriptCustomConversion(
            Type clrDataType,
            Func<object, DynValue?> converter = null
        )
        {
            if (converter == null)
            {
                SetClrToScriptCustomConversion(clrDataType, (Func<Script, object, DynValue?>)null);
                return;
            }

            SetClrToScriptCustomConversion(clrDataType, (s, o) => converter(o));
        }

        /// <summary>
        /// Sets a custom converter from a CLR data type. Set null to remove a previous custom converter.
        /// </summary>
        /// <typeparam name="T">The CLR data type.</typeparam>
        /// <param name="converter">The converter, or null.</param>
        [Obsolete(
            "This method is deprecated. Use the overloads accepting functions with a Script argument."
        )]
        public void SetClrToScriptCustomConversion<T>(Func<T, DynValue?> converter = null)
        {
            if (converter == null)
            {
                SetClrToScriptCustomConversion(typeof(T), (Func<Script, object, DynValue?>)null);
                return;
            }

            SetClrToScriptCustomConversion(typeof(T), o => converter((T)o));
        }

        /// <summary>
        /// Removes all converters.
        /// </summary>
        public void Clear()
        {
            _clr2Script.Clear();

            for (int i = 0; i < _script2Clr.Length; i++)
            {
                _script2Clr[i].Clear();
            }
        }

        /// <summary>
        /// Creates a deep copy of the current converter registry so callers can mutate it independently.
        /// </summary>
        /// <returns>A new <see cref="CustomConverterRegistry"/> containing the same converter mappings.</returns>
        internal CustomConverterRegistry Clone()
        {
            CustomConverterRegistry clone = new();

            for (int i = 0; i < _script2Clr.Length; i++)
            {
                foreach (KeyValuePair<Type, Func<DynValue, object>> pair in _script2Clr[i])
                {
                    clone._script2Clr[i][pair.Key] = pair.Value;
                }
            }

            foreach (KeyValuePair<Type, ClrToScriptConverterEntry> pair in _clr2Script)
            {
                clone._clr2Script[pair.Key] = pair.Value;
            }

            return clone;
        }

        private static bool NormalizeTryConversion(
            ClrToScriptTryConverter converter,
            Script script,
            object value,
            out DynValue result
        )
        {
            if (!converter(script, value, out result))
            {
                result = DynValue.Nil;
                return false;
            }

            return true;
        }
    }
}
