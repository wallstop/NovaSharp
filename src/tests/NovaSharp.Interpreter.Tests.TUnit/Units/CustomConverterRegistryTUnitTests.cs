#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;

    public sealed class CustomConverterRegistryTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ScriptToClrConversionStoresReplacesAndRemovesConverters()
        {
            CustomConverterRegistry registry = new();
            DynValue dynValue = DynValue.NewString("payload");

            Func<DynValue, object> firstConverter = value => value.String + "-first";
            registry.SetScriptToClrCustomConversion(
                DataType.String,
                typeof(string),
                firstConverter
            );

            Func<DynValue, object> resolved = registry.GetScriptToClrCustomConversion(
                DataType.String,
                typeof(string)
            );
            await Assert.That(resolved(dynValue)).IsEqualTo("payload-first");

            Func<DynValue, object> secondConverter = value => value.String + "-second";
            registry.SetScriptToClrCustomConversion(
                DataType.String,
                typeof(string),
                secondConverter
            );

            Func<DynValue, object> updated = registry.GetScriptToClrCustomConversion(
                DataType.String,
                typeof(string)
            );
            await Assert.That(updated(dynValue)).IsEqualTo("payload-second");

            registry.SetScriptToClrCustomConversion(DataType.String, typeof(string));
            await Assert
                .That(
                    registry.GetScriptToClrCustomConversion(DataType.String, typeof(string))
                )
                .IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task SetScriptToClrConversionThrowsWhenTypeExceedsConvertibleRange()
        {
            CustomConverterRegistry registry = new();
            DataType invalidType = (DataType)((int)LuaTypeExtensions.MaxConvertibleTypes + 1);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                registry.SetScriptToClrCustomConversion(
                    invalidType,
                    typeof(string),
                    value => value.String
                )
            );

            await Assert.That(exception.ParamName).IsEqualTo("scriptDataType");
        }

        [global::TUnit.Core.Test]
        public async Task GetScriptToClrConversionReturnsNullOutsideRange()
        {
            CustomConverterRegistry registry = new();
            DataType invalidType = (DataType)((int)LuaTypeExtensions.MaxConvertibleTypes + 1);

            Func<DynValue, object> result = registry.GetScriptToClrCustomConversion(
                invalidType,
                typeof(string)
            );

            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task ClrToScriptConversionRegistersAndRemovesDelegates()
        {
            CustomConverterRegistry registry = new();
            Script script = new();

            Func<Script, string, DynValue> converter = (s, value) =>
                DynValue.NewString(value + "-converted");
            registry.SetClrToScriptCustomConversion(converter);

            Func<Script, object, DynValue> resolved = registry.GetClrToScriptCustomConversion(
                typeof(string)
            );
            await Assert.That(resolved).IsNotNull();
            DynValue converted = resolved(script, "value");
            await Assert.That(converted.String).IsEqualTo("value-converted");

            registry.SetClrToScriptCustomConversion(
                typeof(string),
                (Func<Script, object, DynValue>)null
            );
            await Assert.That(registry.GetClrToScriptCustomConversion(typeof(string))).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task TypedClrToScriptConversionUsesStronglyTypedDelegate()
        {
            CustomConverterRegistry registry = new();
            Script script = new();
            registry.SetClrToScriptCustomConversion<int>(
                (s, number) =>
                {
                    return DynValue.NewNumber(number + 5);
                }
            );

            Func<Script, object, DynValue> resolved = registry.GetClrToScriptCustomConversion(
                typeof(int)
            );
            DynValue result = resolved(script, 10);

            await Assert.That(result.Number).IsEqualTo(15d);
        }

        [global::TUnit.Core.Test]
        public async Task ObsoleteClrToScriptConversionOverloadsBridgeToScriptAwareDelegates()
        {
            CustomConverterRegistry registry = new();
            Script script = new();
            Guid sampleGuid = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

            InvokeLegacyClrToScriptConversion(
                registry,
                typeof(Guid),
                value => DynValue.NewString(((Guid)value).ToString("N"))
            );
            InvokeLegacyClrToScriptConversion<long>(
                registry,
                value => DynValue.NewNumber(value * 2)
            );

            Func<Script, object, DynValue> guidConverter = registry.GetClrToScriptCustomConversion(
                typeof(Guid)
            );
            DynValue guidResult = guidConverter(script, sampleGuid);
            await Assert.That(guidResult.String).IsEqualTo(sampleGuid.ToString("N"));

            Func<Script, object, DynValue> longConverter = registry.GetClrToScriptCustomConversion(
                typeof(long)
            );
            DynValue longResult = longConverter(script, 7L);
            await Assert.That(longResult.Number).IsEqualTo(14d);

            registry.SetClrToScriptCustomConversion(
                typeof(Guid),
                (Func<Script, object, DynValue>)null
            );
            registry.SetClrToScriptCustomConversion<long>((Func<Script, long, DynValue>)null);

            await Assert.That(registry.GetClrToScriptCustomConversion(typeof(Guid))).IsNull();
            await Assert.That(registry.GetClrToScriptCustomConversion(typeof(long))).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task ScriptAwareTypedClrToScriptConversionRemovesWhenNull()
        {
            CustomConverterRegistry registry = new();
            registry.SetClrToScriptCustomConversion<int>(
                (script, value) =>
                {
                    return DynValue.NewNumber(value);
                }
            );

            await Assert.That(registry.GetClrToScriptCustomConversion(typeof(int))).IsNotNull();

            registry.SetClrToScriptCustomConversion<int>((Func<Script, int, DynValue>)null);

            await Assert.That(registry.GetClrToScriptCustomConversion(typeof(int))).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task ClearRemovesAllConverters()
        {
            CustomConverterRegistry registry = new();
            registry.SetScriptToClrCustomConversion(
                DataType.String,
                typeof(string),
                value => value.String
            );
            registry.SetClrToScriptCustomConversion(
                typeof(int),
                (s, value) => DynValue.NewNumber((int)value)
            );

            registry.Clear();

            await Assert
                .That(
                    registry.GetScriptToClrCustomConversion(DataType.String, typeof(string))
                )
                .IsNull();
            await Assert.That(registry.GetClrToScriptCustomConversion(typeof(int))).IsNull();
        }

        private static readonly MethodInfo LegacyClrToScriptConversionMethod =
            ResolveLegacyClrToScriptConversionMethod();

        private static readonly MethodInfo LegacyTypedClrToScriptConversionMethod =
            ResolveLegacyTypedClrToScriptConversionMethod();

        private static void InvokeLegacyClrToScriptConversion(
            CustomConverterRegistry registry,
            Type clrType,
            Func<object, DynValue> converter
        )
        {
            LegacyClrToScriptConversionMethod.Invoke(registry, new object[] { clrType, converter });
        }

        private static void InvokeLegacyClrToScriptConversion<T>(
            CustomConverterRegistry registry,
            Func<T, DynValue> converter
        )
        {
            MethodInfo method = LegacyTypedClrToScriptConversionMethod.MakeGenericMethod(typeof(T));
            method.Invoke(registry, new object[] { converter });
        }

        private static MethodInfo ResolveLegacyClrToScriptConversionMethod()
        {
            MethodInfo method = typeof(CustomConverterRegistry).GetMethod(
                nameof(CustomConverterRegistry.SetClrToScriptCustomConversion),
                new[] { typeof(Type), typeof(Func<object, DynValue>) }
            );

            if (method == null)
            {
                throw new InvalidOperationException(
                    "Could not locate the legacy SetClrToScriptCustomConversion(Type, Func<object, DynValue>) overload."
                );
            }

            return method;
        }

        private static MethodInfo ResolveLegacyTypedClrToScriptConversionMethod()
        {
            MethodInfo[] candidates = typeof(CustomConverterRegistry).GetMethods();
            foreach (MethodInfo method in candidates)
            {
                if (
                    !method.IsGenericMethodDefinition
                    || method.Name != nameof(CustomConverterRegistry.SetClrToScriptCustomConversion)
                )
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    continue;
                }

                Type parameterType = parameters[0].ParameterType;
                if (
                    parameterType.IsGenericType
                    && parameterType.GetGenericTypeDefinition() == typeof(Func<,>)
                )
                {
                    return method;
                }
            }

            throw new InvalidOperationException(
                "Could not locate the legacy SetClrToScriptCustomConversion<T>(Func<T, DynValue>) overload."
            );
        }
    }
}
#pragma warning restore CA2007
