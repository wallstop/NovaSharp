namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class OsTimeModuleTests
    {
        [Test]
        public void TimeReturnsUnixSecondsForTableInput()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                return os.time({
                    year = 1970,
                    month = 1,
                    day = 2,
                    hour = 0,
                    min = 0,
                    sec = 0
                })
                "
            );

            Assert.That(result.Number, Is.EqualTo(86400.0).Within(0.001));
        }

        [Test]
        public void TimeThrowsWhenDayFieldMissing()
        {
            Script script = CreateScript();

            Assert.That(
                () =>
                    script.DoString(
                        @"
                    return os.time({
                        year = 1985,
                        month = 5
                    })
                    "
                    ),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contain("field 'day' missing in date table")
            );
        }

        [Test]
        public void DifftimeHandlesOptionalStartArgument()
        {
            Script script = CreateScript();
            DynValue diff = script.DoString("return os.difftime(200, 150)");
            DynValue diffFromZero = script.DoString("return os.difftime(200)");

            Assert.Multiple(() =>
            {
                Assert.That(diff.Number, Is.EqualTo(50));
                Assert.That(diffFromZero.Number, Is.EqualTo(200));
            });
        }

        [Test]
        public void DateFormatsUtcTimestampWhenPrefixedWithBang()
        {
            Script script = CreateScript();
            DynValue formatted = script.DoString(
                "return os.date('!%Y-%m-%d %H:%M:%S', 1609459200)"
            );

            Assert.That(formatted.String, Is.EqualTo("2021-01-01 00:00:00"));
        }

        [Test]
        public void DateReturnsTableWhenRequested()
        {
            Script script = CreateScript();
            DynValue tableValue = script.DoString("return os.date('!*t', 1609459200)");

            Assert.That(tableValue.Type, Is.EqualTo(DataType.Table));
            Assert.Multiple(() =>
            {
                Assert.That(tableValue.Table.Get("year").Number, Is.EqualTo(2021));
                Assert.That(tableValue.Table.Get("month").Number, Is.EqualTo(1));
                Assert.That(tableValue.Table.Get("day").Number, Is.EqualTo(1));
                Assert.That(tableValue.Table.Get("isdst").Boolean, Is.False);
            });
        }

        [Test]
        public void DateReturnsLocalTableWhenPrefixOmitted()
        {
            DateTime localTime = DateTimeOffset.FromUnixTimeSeconds(1609459200).LocalDateTime;

            Script script = CreateScript();
            DynValue tableValue = script.DoString("return os.date('*t', 1609459200)");

            Assert.That(tableValue.Type, Is.EqualTo(DataType.Table));
            Assert.Multiple(() =>
            {
                Assert.That(tableValue.Table.Get("year").Number, Is.EqualTo(localTime.Year));
                Assert.That(tableValue.Table.Get("month").Number, Is.EqualTo(localTime.Month));
                Assert.That(tableValue.Table.Get("day").Number, Is.EqualTo(localTime.Day));
                Assert.That(tableValue.Table.Get("hour").Number, Is.EqualTo(localTime.Hour));
                Assert.That(tableValue.Table.Get("min").Number, Is.EqualTo(localTime.Minute));
                Assert.That(tableValue.Table.Get("sec").Number, Is.EqualTo(localTime.Second));
                Assert.That(
                    tableValue.Table.Get("isdst").Boolean,
                    Is.EqualTo(localTime.IsDaylightSavingTime())
                );
            });
        }

        [Test]
        public void DateReturnsPlaceholderForUnsupportedWeekPatterns()
        {
            Script script = CreateScript();
            DynValue value = script.DoString("return os.date('%U-%V-%W', 1609459200)");

            Assert.That(value.String, Is.EqualTo("??-??-??"));
        }

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            return script;
        }
    }
}
