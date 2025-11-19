namespace NovaSharp.Interpreter.Interop.Converters
{
    using System;
    using System.Globalization;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;

    internal static class ScriptToClrConversions
    {
        internal const int WeightMaxValue = 100;
        internal const int WeightCustomConverterMatch = 100;
        internal const int WeightExactMatch = 100;
        internal const int WeightStringToStringBuilder = 99;
        internal const int WeightStringToChar = 98;
        internal const int WeightNilToNullable = 100;
        internal const int WeightNilToRefType = 100;
        internal const int WeightVoidWithDefault = 50;
        internal const int WeightVoidWithoutDefault = 25;
        internal const int WeightNilWithDefault = 25;
        internal const int WeightBoolToString = 5;
        internal const int WeightNumberToString = 50;
        internal const int WeightNumberToEnum = 90;
        internal const int WeightUserDataToString = 5;
        internal const int WeightTableConversion = 90;
        internal const int WeightNumberDowncast = 99;
        internal const int WeightNoMatch = 0;
        internal const int WeightNoExtraParamsBonus = 100;
        internal const int WeightExtraParamsMalus = 2;
        internal const int WeightByRefBonusMalus = -10;
        internal const int WeightVarArgsMalus = 1;
        internal const int WeightVarArgsEmpty = 40;

        /// <summary>
        /// Converts a DynValue to a CLR object [simple conversion]
        /// </summary>
        internal static object DynValueToObject(DynValue value)
        {
            Func<DynValue, object> converter =
                Script.GlobalOptions.CustomConverters.GetScriptToClrCustomConversion(
                    value.Type,
                    typeof(Object)
                );
            if (converter != null)
            {
                object v = converter(value);
                if (v != null)
                {
                    return v;
                }
            }

            switch (value.Type)
            {
                case DataType.Void:
                case DataType.Nil:
                    return null;
                case DataType.Boolean:
                    return value.Boolean;
                case DataType.Number:
                    return value.Number;
                case DataType.String:
                    return value.String;
                case DataType.Function:
                    return value.Function;
                case DataType.Table:
                    return value.Table;
                case DataType.Tuple:
                    return value.Tuple;
                case DataType.UserData:
                    if (value.UserData.Object != null)
                    {
                        return value.UserData.Object;
                    }
                    else if (value.UserData.Descriptor != null)
                    {
                        return value.UserData.Descriptor.Type;
                    }
                    else
                    {
                        return null;
                    }
                case DataType.ClrFunction:
                    return value.Callback;
                default:
                    throw ScriptRuntimeException.ConvertObjectFailed(value.Type);
            }
        }

        /// <summary>
        /// Converts a DynValue to a CLR object of a specific type
        /// </summary>
        internal static object DynValueToObjectOfType(
            DynValue value,
            Type desiredType,
            object defaultValue,
            bool isOptional
        )
        {
            if (desiredType.IsByRef)
            {
                desiredType = desiredType.GetElementType();
            }

            Func<DynValue, object> converter =
                Script.GlobalOptions.CustomConverters.GetScriptToClrCustomConversion(
                    value.Type,
                    desiredType
                );
            if (converter != null)
            {
                object v = converter(value);
                if (v != null)
                {
                    return v;
                }
            }

            if (desiredType == typeof(DynValue))
            {
                return value;
            }

            if (desiredType == typeof(object))
            {
                return DynValueToObject(value);
            }

            StringConversions.StringSubtype stringSubType = StringConversions.GetStringSubtype(
                desiredType
            );
            string str = null;

            Type nt = Nullable.GetUnderlyingType(desiredType);
            Type nullableType = null;

            if (nt != null)
            {
                nullableType = desiredType;
                desiredType = nt;
            }

            switch (value.Type)
            {
                case DataType.Void:
                    if (isOptional)
                    {
                        return defaultValue;
                    }
                    else if ((!Framework.Do.IsValueType(desiredType)) || (nullableType != null))
                    {
                        return null;
                    }

                    break;
                case DataType.Nil:
                    if (Framework.Do.IsValueType(desiredType))
                    {
                        if (nullableType != null)
                        {
                            return null;
                        }

                        if (isOptional)
                        {
                            return defaultValue;
                        }
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case DataType.Boolean:
                    if (desiredType == typeof(bool))
                    {
                        return value.Boolean;
                    }

                    if (stringSubType != default)
                    {
                        str = value.Boolean.ToString();
                    }

                    break;
                case DataType.Number:
                    if (Framework.Do.IsEnum(desiredType))
                    { // number to enum conv
                        Type underType = Enum.GetUnderlyingType(desiredType);
                        return NumericConversions.DoubleToType(underType, value.Number);
                    }
                    if (NumericConversions.NumericTypes.Contains(desiredType))
                    {
                        object d = NumericConversions.DoubleToType(desiredType, value.Number);
                        if (d.GetType() == desiredType)
                        {
                            return d;
                        }

                        break;
                    }
                    if (stringSubType != default)
                    {
                        str = value.Number.ToString(CultureInfo.InvariantCulture);
                    }

                    break;
                case DataType.String:
                    if (stringSubType != default)
                    {
                        str = value.String;
                    }

                    break;
                case DataType.Function:
                    if (desiredType == typeof(Closure))
                    {
                        return value.Function;
                    }
                    else if (desiredType == typeof(ScriptFunctionDelegate))
                    {
                        return value.Function.GetDelegate();
                    }

                    break;
                case DataType.ClrFunction:
                    if (desiredType == typeof(CallbackFunction))
                    {
                        return value.Callback;
                    }
                    else if (
                        desiredType
                        == typeof(Func<ScriptExecutionContext, CallbackArguments, DynValue>)
                    )
                    {
                        return value.Callback.ClrCallback;
                    }

                    break;
                case DataType.UserData:
                    if (value.UserData.Object != null)
                    {
                        object udObj = value.UserData.Object;
                        IUserDataDescriptor udDesc = value.UserData.Descriptor;

                        if (udDesc.IsTypeCompatible(desiredType, udObj))
                        {
                            return udObj;
                        }

                        if (stringSubType != default)
                        {
                            str = udDesc.AsString(udObj);
                        }
                    }
                    break;
                case DataType.Table:
                    if (
                        desiredType == typeof(Table)
                        || Framework.Do.IsAssignableFrom(desiredType, typeof(Table))
                    )
                    {
                        return value.Table;
                    }
                    else
                    {
                        object o = TableConversions.ConvertTableToType(value.Table, desiredType);
                        if (o != null)
                        {
                            return o;
                        }
                    }
                    break;
                case DataType.Tuple:
                    break;
            }

            if (stringSubType != default && str != null)
            {
                return StringConversions.ConvertString(stringSubType, str, desiredType, value.Type);
            }

            throw ScriptRuntimeException.ConvertObjectFailed(value.Type, desiredType);
        }

        /// <summary>
        /// Gets a relative weight of how much the conversion is matching the given types.
        /// Implementation must follow that of DynValueToObjectOfType.. it's not very DRY in that sense.
        /// However here we are in perf-sensitive path.. TODO : double-check the gain and see if a DRY impl is better.
        /// </summary>
        internal static int DynValueToObjectOfTypeWeight(
            DynValue value,
            Type desiredType,
            bool isOptional
        )
        {
            if (desiredType.IsByRef)
            {
                desiredType = desiredType.GetElementType();
            }

            Func<DynValue, object> customConverter =
                Script.GlobalOptions.CustomConverters.GetScriptToClrCustomConversion(
                    value.Type,
                    desiredType
                );
            if (customConverter != null)
            {
                return WeightCustomConverterMatch;
            }

            if (desiredType == typeof(DynValue))
            {
                return WeightExactMatch;
            }

            if (desiredType == typeof(object))
            {
                return WeightExactMatch;
            }

            StringConversions.StringSubtype stringSubType = StringConversions.GetStringSubtype(
                desiredType
            );

            Type nt = Nullable.GetUnderlyingType(desiredType);
            Type nullableType = null;

            if (nt != null)
            {
                nullableType = desiredType;
                desiredType = nt;
            }

            switch (value.Type)
            {
                case DataType.Void:
                    if (isOptional)
                    {
                        return WeightVoidWithDefault;
                    }
                    else if ((!Framework.Do.IsValueType(desiredType)) || (nullableType != null))
                    {
                        return WeightVoidWithoutDefault;
                    }

                    break;
                case DataType.Nil:
                    if (Framework.Do.IsValueType(desiredType))
                    {
                        if (nullableType != null)
                        {
                            return WeightNilToNullable;
                        }

                        if (isOptional)
                        {
                            return WeightNilWithDefault;
                        }
                    }
                    else
                    {
                        return WeightNilToRefType;
                    }
                    break;
                case DataType.Boolean:
                    if (desiredType == typeof(bool))
                    {
                        return WeightExactMatch;
                    }

                    if (stringSubType != default)
                    {
                        return WeightBoolToString;
                    }

                    break;
                case DataType.Number:
                    if (Framework.Do.IsEnum(desiredType))
                    { // number to enum conv
                        return WeightNumberToEnum;
                    }
                    if (NumericConversions.NumericTypes.Contains(desiredType))
                    {
                        return GetNumericTypeWeight(desiredType);
                    }

                    if (stringSubType != default)
                    {
                        return WeightNumberToString;
                    }

                    break;
                case DataType.String:
                    if (stringSubType == StringConversions.StringSubtype.String)
                    {
                        return WeightExactMatch;
                    }
                    else if (stringSubType == StringConversions.StringSubtype.StringBuilder)
                    {
                        return WeightStringToStringBuilder;
                    }
                    else if (stringSubType == StringConversions.StringSubtype.Char)
                    {
                        return WeightStringToChar;
                    }

                    break;
                case DataType.Function:
                    if (desiredType == typeof(Closure))
                    {
                        return WeightExactMatch;
                    }
                    else if (desiredType == typeof(ScriptFunctionDelegate))
                    {
                        return WeightExactMatch;
                    }

                    break;
                case DataType.ClrFunction:
                    if (desiredType == typeof(CallbackFunction))
                    {
                        return WeightExactMatch;
                    }
                    else if (
                        desiredType
                        == typeof(Func<ScriptExecutionContext, CallbackArguments, DynValue>)
                    )
                    {
                        return WeightExactMatch;
                    }

                    break;
                case DataType.UserData:
                    if (value.UserData.Object != null)
                    {
                        object udObj = value.UserData.Object;
                        IUserDataDescriptor udDesc = value.UserData.Descriptor;

                        if (udDesc.IsTypeCompatible(desiredType, udObj))
                        {
                            return WeightExactMatch;
                        }

                        if (stringSubType != default)
                        {
                            return WeightUserDataToString;
                        }
                    }
                    break;
                case DataType.Table:
                    if (
                        desiredType == typeof(Table)
                        || Framework.Do.IsAssignableFrom(desiredType, typeof(Table))
                    )
                    {
                        return WeightExactMatch;
                    }
                    else if (TableConversions.CanConvertTableToType(value.Table, desiredType))
                    {
                        return WeightTableConversion;
                    }

                    break;
                case DataType.Tuple:
                    break;
            }

            return WeightNoMatch;
        }

        private static int GetNumericTypeWeight(Type desiredType)
        {
            if (desiredType == typeof(double) || desiredType == typeof(decimal))
            {
                return WeightExactMatch;
            }
            else
            {
                return WeightNumberDowncast;
            }
        }
    }
}
