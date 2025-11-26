namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CustomConverterRegistryTests
    {
        [Test]
        public void ScriptToClrConversionStoresReplacesAndRemovesConverters()
        {
            CustomConverterRegistry registry = new CustomConverterRegistry();
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
            Assert.That(resolved(dynValue), Is.EqualTo("payload-first"));

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
            Assert.That(updated(dynValue), Is.EqualTo("payload-second"));

            registry.SetScriptToClrCustomConversion(DataType.String, typeof(string));
            Assert.That(
                registry.GetScriptToClrCustomConversion(DataType.String, typeof(string)),
                Is.Null
            );
        }

        [Test]
        public void SetScriptToClrConversionThrowsWhenTypeExceedsConvertibleRange()
        {
            CustomConverterRegistry registry = new CustomConverterRegistry();
            DataType invalidType = (DataType)((int)LuaTypeExtensions.MaxConvertibleTypes + 1);

            Assert.Throws<ArgumentException>(() =>
                registry.SetScriptToClrCustomConversion(
                    invalidType,
                    typeof(string),
                    value => value.String
                )
            );
        }

        [Test]
        public void GetScriptToClrConversionReturnsNullOutsideRange()
        {
            CustomConverterRegistry registry = new CustomConverterRegistry();
            DataType invalidType = (DataType)((int)LuaTypeExtensions.MaxConvertibleTypes + 1);

            Func<DynValue, object> result = registry.GetScriptToClrCustomConversion(
                invalidType,
                typeof(string)
            );

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ClrToScriptConversionRegistersAndRemovesDelegates()
        {
            CustomConverterRegistry registry = new CustomConverterRegistry();
            Script script = new Script();

            Func<Script, string, DynValue> converter = (s, value) =>
                DynValue.NewString(value + "-converted");
            registry.SetClrToScriptCustomConversion(converter);

            Func<Script, object, DynValue> resolved = registry.GetClrToScriptCustomConversion(
                typeof(string)
            );
            Assert.That(resolved, Is.Not.Null);
            DynValue converted = resolved(script, "value");
            Assert.That(converted.String, Is.EqualTo("value-converted"));

            registry.SetClrToScriptCustomConversion(
                typeof(string),
                (Func<Script, object, DynValue>)null
            );
            Assert.That(registry.GetClrToScriptCustomConversion(typeof(string)), Is.Null);
        }

        [Test]
        public void TypedClrToScriptConversionUsesStronglyTypedDelegate()
        {
            CustomConverterRegistry registry = new CustomConverterRegistry();
            Script script = new Script();
            registry.SetClrToScriptCustomConversion<int>(
                (s, number) =>
                {
                    Assert.That(s, Is.SameAs(script));
                    return DynValue.NewNumber(number + 5);
                }
            );

            Func<Script, object, DynValue> resolved = registry.GetClrToScriptCustomConversion(
                typeof(int)
            );
            DynValue result = resolved(script, 10);

            Assert.That(result.Number, Is.EqualTo(15));
        }

        [Test]
        public void ObsoleteClrToScriptConversionOverloadsBridgeToScriptAwareDelegates()
        {
            CustomConverterRegistry registry = new CustomConverterRegistry();
            Script script = new Script();
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
            Assert.That(guidResult.String, Is.EqualTo(sampleGuid.ToString("N")));

            Func<Script, object, DynValue> longConverter = registry.GetClrToScriptCustomConversion(
                typeof(long)
            );
            DynValue longResult = longConverter(script, 7L);
            Assert.That(longResult.Number, Is.EqualTo(14));

            registry.SetClrToScriptCustomConversion(
                typeof(Guid),
                (Func<Script, object, DynValue>)null
            );
            registry.SetClrToScriptCustomConversion<long>((Func<Script, long, DynValue>)null);

            Assert.Multiple(() =>
            {
                Assert.That(registry.GetClrToScriptCustomConversion(typeof(Guid)), Is.Null);
                Assert.That(registry.GetClrToScriptCustomConversion(typeof(long)), Is.Null);
            });
        }

        [Test]
        public void ScriptAwareTypedClrToScriptConversionRemovesWhenNull()
        {
            CustomConverterRegistry registry = new CustomConverterRegistry();
            registry.SetClrToScriptCustomConversion<int>(
                (script, value) =>
                {
                    Assert.That(script, Is.Not.Null);
                    return DynValue.NewNumber(value);
                }
            );

            Assert.That(registry.GetClrToScriptCustomConversion(typeof(int)), Is.Not.Null);

            registry.SetClrToScriptCustomConversion<int>((Func<Script, int, DynValue>)null);

            Assert.That(registry.GetClrToScriptCustomConversion(typeof(int)), Is.Null);
        }

        [Test]
        public void ClearRemovesAllConverters()
        {
            CustomConverterRegistry registry = new CustomConverterRegistry();
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

            Assert.Multiple(() =>
            {
                Assert.That(
                    registry.GetScriptToClrCustomConversion(DataType.String, typeof(string)),
                    Is.Null
                );
                Assert.That(registry.GetClrToScriptCustomConversion(typeof(int)), Is.Null);
            });
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
