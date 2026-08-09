namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Converters
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop;

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
            await Assert.That(resolved(dynValue)).IsEqualTo("payload-first").ConfigureAwait(false);

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
            await Assert.That(updated(dynValue)).IsEqualTo("payload-second").ConfigureAwait(false);

            registry.SetScriptToClrCustomConversion(DataType.String, typeof(string));
            await Assert
                .That(registry.GetScriptToClrCustomConversion(DataType.String, typeof(string)))
                .IsNull()
                .ConfigureAwait(false);
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

            await Assert
                .That(exception.ParamName)
                .IsEqualTo("scriptDataType")
                .ConfigureAwait(false);
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

            await Assert.That(result).IsNull().ConfigureAwait(false);
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
            await Assert.That(resolved).IsNotNull().ConfigureAwait(false);
            DynValue converted = resolved(script, "value");
            await Assert.That(converted.String).IsEqualTo("value-converted").ConfigureAwait(false);

            ClrToScriptTryConverter adapted = registry.GetClrToScriptTryConversion(typeof(string));
            await Assert.That(adapted).IsNotNull().ConfigureAwait(false);
            bool handled = adapted(script, "value", out DynValue adaptedValue);
            await Assert.That(handled).IsTrue().ConfigureAwait(false);
            await Assert
                .That(adaptedValue.String)
                .IsEqualTo("value-converted")
                .ConfigureAwait(false);
            bool handledDirectly = registry.TryConvertClrToScript(
                typeof(string),
                script,
                "value",
                out DynValue directValue
            );
            await Assert.That(handledDirectly).IsTrue().ConfigureAwait(false);
            await Assert
                .That(directValue.String)
                .IsEqualTo("value-converted")
                .ConfigureAwait(false);

            registry.SetClrToScriptCustomConversion(typeof(string), (Script _, object _) => null);
            ClrToScriptTryConverter decliningAdapter = registry.GetClrToScriptTryConversion(
                typeof(string)
            );
            bool declined = decliningAdapter(script, "value", out DynValue declinedValue);
            await Assert.That(declined).IsFalse().ConfigureAwait(false);
            await Assert.That(declinedValue.IsNil()).IsTrue().ConfigureAwait(false);

            registry.SetClrToScriptCustomConversion(
                typeof(string),
                (Func<Script, object, DynValue>)null
            );
            await Assert
                .That(registry.GetClrToScriptCustomConversion(typeof(string)))
                .IsNull()
                .ConfigureAwait(false);
            await Assert
                .That(registry.GetClrToScriptTryConversion(typeof(string)))
                .IsNull()
                .ConfigureAwait(false);
            bool missing = registry.TryConvertClrToScript(
                typeof(string),
                script,
                "value",
                out DynValue missingValue
            );
            await Assert.That(missing).IsFalse().ConfigureAwait(false);
            await Assert.That(missingValue.IsNil()).IsTrue().ConfigureAwait(false);
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

            await Assert.That(result.Number).IsEqualTo(15d).ConfigureAwait(false);

            registry.SetClrToScriptTryConversion<int>(
                (Script _, int number, out DynValue converted) =>
                {
                    if (number == 10)
                    {
                        converted = DynValue.Nil;
                        return true;
                    }

                    if (number == 11)
                    {
                        converted = DynValue.Void;
                        return true;
                    }

                    converted = DynValue.NewNumber(number);
                    return false;
                }
            );

            ClrToScriptTryConverter tryConverter = registry.GetClrToScriptTryConversion(
                typeof(int)
            );
            bool handledNil = tryConverter(script, 10, out DynValue nilResult);
            bool handledVoid = tryConverter(script, 11, out DynValue voidResult);
            bool declined = tryConverter(script, 12, out DynValue declinedResult);

            await Assert.That(handledNil).IsTrue().ConfigureAwait(false);
            await Assert.That(nilResult.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(handledVoid).IsTrue().ConfigureAwait(false);
            await Assert.That(voidResult.IsVoid()).IsTrue().ConfigureAwait(false);
            await Assert.That(declined).IsFalse().ConfigureAwait(false);
            await Assert.That(declinedResult.IsNil()).IsTrue().ConfigureAwait(false);

            Func<Script, object, DynValue> legacyView = registry.GetClrToScriptCustomConversion(
                typeof(int)
            );
            await Assert.That(legacyView(script, 10).IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(legacyView(script, 11).IsVoid()).IsTrue().ConfigureAwait(false);
            await Assert.That(legacyView(script, 12)).IsNull().ConfigureAwait(false);

            registry.SetClrToScriptTryConversion<int>(
                (Script _, int _, out DynValue converted) =>
                {
                    converted = null;
                    return true;
                }
            );
            ClrToScriptTryConverter invalidConverter = registry.GetClrToScriptTryConversion(
                typeof(int)
            );
            await Assert
                .That(() => invalidConverter(script, 10, out DynValue _))
                .Throws<InvalidOperationException>()
                .ConfigureAwait(false);
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
            await Assert
                .That(guidResult.String)
                .IsEqualTo(sampleGuid.ToString("N"))
                .ConfigureAwait(false);

            Func<Script, object, DynValue> longConverter = registry.GetClrToScriptCustomConversion(
                typeof(long)
            );
            DynValue longResult = longConverter(script, 7L);
            await Assert.That(longResult.Number).IsEqualTo(14d).ConfigureAwait(false);

            registry.SetClrToScriptCustomConversion(
                typeof(Guid),
                (Func<Script, object, DynValue>)null
            );
            registry.SetClrToScriptCustomConversion<long>((Func<Script, long, DynValue>)null);

            await Assert
                .That(registry.GetClrToScriptCustomConversion(typeof(Guid)))
                .IsNull()
                .ConfigureAwait(false);
            await Assert
                .That(registry.GetClrToScriptCustomConversion(typeof(long)))
                .IsNull()
                .ConfigureAwait(false);
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

            await Assert
                .That(registry.GetClrToScriptCustomConversion(typeof(int)))
                .IsNotNull()
                .ConfigureAwait(false);

            registry.SetClrToScriptCustomConversion<int>((Func<Script, int, DynValue>)null);

            await Assert
                .That(registry.GetClrToScriptCustomConversion(typeof(int)))
                .IsNull()
                .ConfigureAwait(false);

            registry.SetClrToScriptTryConversion<int>(
                (Script _, int value, out DynValue result) =>
                {
                    result = DynValue.NewNumber(value);
                    return true;
                }
            );
            await Assert
                .That(registry.GetClrToScriptTryConversion(typeof(int)))
                .IsNotNull()
                .ConfigureAwait(false);

            registry.SetClrToScriptTryConversion<int>(null);

            await Assert
                .That(registry.GetClrToScriptCustomConversion(typeof(int)))
                .IsNull()
                .ConfigureAwait(false);
            await Assert
                .That(registry.GetClrToScriptTryConversion(typeof(int)))
                .IsNull()
                .ConfigureAwait(false);
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
            registry.SetClrToScriptTryConversion<Guid>(
                (Script _, Guid _, out DynValue result) =>
                {
                    result = DynValue.Void;
                    return true;
                }
            );

            CustomConverterRegistry clone = registry.Clone();

            registry.Clear();

            await Assert
                .That(registry.GetScriptToClrCustomConversion(DataType.String, typeof(string)))
                .IsNull()
                .ConfigureAwait(false);
            await Assert
                .That(registry.GetClrToScriptCustomConversion(typeof(int)))
                .IsNull()
                .ConfigureAwait(false);
            await Assert
                .That(registry.GetClrToScriptTryConversion(typeof(Guid)))
                .IsNull()
                .ConfigureAwait(false);

            Func<DynValue, object> clonedScriptConverter = clone.GetScriptToClrCustomConversion(
                DataType.String,
                typeof(string)
            );
            await Assert
                .That(clonedScriptConverter(DynValue.NewString("preserved")))
                .IsEqualTo("preserved")
                .ConfigureAwait(false);

            ClrToScriptTryConverter clonedLegacyConverter = clone.GetClrToScriptTryConversion(
                typeof(int)
            );
            bool handledLegacy = clonedLegacyConverter(
                new Script(),
                7,
                out DynValue clonedLegacyResult
            );
            await Assert.That(handledLegacy).IsTrue().ConfigureAwait(false);
            await Assert.That(clonedLegacyResult.Number).IsEqualTo(7d).ConfigureAwait(false);

            ClrToScriptTryConverter clonedTryConverter = clone.GetClrToScriptTryConversion(
                typeof(Guid)
            );
            bool handledTry = clonedTryConverter(
                new Script(),
                Guid.Empty,
                out DynValue clonedTryResult
            );
            await Assert.That(handledTry).IsTrue().ConfigureAwait(false);
            await Assert.That(clonedTryResult.IsVoid()).IsTrue().ConfigureAwait(false);
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

        [global::TUnit.Core.Test]
        public async Task ObsoleteTypedClrToScriptConversionNullBehavior()
        {
            CustomConverterRegistry registry = new();
            Type stringType = typeof(string);
            InvokeLegacyClrToScriptConversion<double>(registry, value => DynValue.NewNumber(value));
            InvokeLegacyClrToScriptConversion(
                registry,
                stringType,
                value => DynValue.NewString((string)value)
            );

            await Assert
                .That(registry.GetClrToScriptCustomConversion(typeof(double)))
                .IsNotNull()
                .ConfigureAwait(false);
            await Assert
                .That(registry.GetClrToScriptCustomConversion(stringType))
                .IsNotNull()
                .ConfigureAwait(false);

            InvokeLegacyClrToScriptConversion<double>(registry, null);
            InvokeLegacyClrToScriptConversion(registry, stringType, null);

            await Assert
                .That(registry.GetClrToScriptCustomConversion(typeof(double)))
                .IsNull()
                .ConfigureAwait(false);
            await Assert
                .That(registry.GetClrToScriptTryConversion(typeof(double)))
                .IsNull()
                .ConfigureAwait(false);
            await Assert
                .That(registry.GetClrToScriptCustomConversion(stringType))
                .IsNull()
                .ConfigureAwait(false);
            await Assert
                .That(registry.GetClrToScriptTryConversion(stringType))
                .IsNull()
                .ConfigureAwait(false);
        }
    }
}
