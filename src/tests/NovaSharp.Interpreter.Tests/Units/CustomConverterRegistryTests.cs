namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
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

#pragma warning disable CS0618
            registry.SetClrToScriptCustomConversion(
                typeof(Guid),
                value => DynValue.NewString(((Guid)value).ToString("N"))
            );
            registry.SetClrToScriptCustomConversion<long>(value => DynValue.NewNumber(value * 2));
#pragma warning restore CS0618

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
    }
}
