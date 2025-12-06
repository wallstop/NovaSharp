namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    [UserDataIsolation]
    public sealed class OsTimeModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TimeReturnsUnixSecondsForTableInput()
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

            await Assert.That(Math.Abs(result.Number - 86400.0)).IsLessThanOrEqualTo(0.001);
        }

        [global::TUnit.Core.Test]
        public async Task TimeThrowsWhenDayFieldMissing()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    return os.time({
                        year = 1985,
                        month = 5
                    })
                    "
                )
            );

            await Assert.That(exception.Message).Contains("field 'day' missing in date table");
        }

        [global::TUnit.Core.Test]
        public async Task TimeThrowsWhenMonthFieldMissing()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    return os.time({
                        year = 1985,
                        day = 12
                    })
                    "
                )
            );

            await Assert.That(exception.Message).Contains("field 'month' missing in date table");
        }

        [global::TUnit.Core.Test]
        public async Task TimeThrowsWhenYearFieldMissing()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    return os.time({
                        month = 5,
                        day = 12
                    })
                    "
                )
            );

            await Assert.That(exception.Message).Contains("field 'year' missing in date table");
        }

        [global::TUnit.Core.Test]
        public async Task TimeReturnsNilForDatesBeforeEpoch()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                return os.time({
                    year = 1969,
                    month = 12,
                    day = 31,
                    hour = 23,
                    min = 59,
                    sec = 59
                })
                "
            );

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ClockReturnsElapsedSeconds()
        {
            Script script = CreateScript();
            DynValue elapsed = script.DoString("return os.clock()");

            await Assert.That(elapsed.Number).IsGreaterThanOrEqualTo(0.0);
        }

        [global::TUnit.Core.Test]
        public async Task ClockReturnsZeroWhenTimeProviderMovesBackward()
        {
            DateTimeOffset later = DateTimeOffset.FromUnixTimeSeconds(1_000_000);
            DateTimeOffset earlier = DateTimeOffset.FromUnixTimeSeconds(999_990);
            Script script = CreateScriptWithTimeProvider(later, earlier);

            DynValue elapsed = script.DoString("return os.clock()");

            await Assert.That(elapsed.Number).IsEqualTo(0.0);
        }

        [global::TUnit.Core.Test]
        public async Task DiffTimeHandlesOptionalStartArgument()
        {
            Script script = CreateScript();
            DynValue diff = script.DoString("return os.difftime(200, 150)");
            DynValue diffFromZero = script.DoString("return os.difftime(200)");

            await Assert.That(diff.Number).IsEqualTo(50);
            await Assert.That(diffFromZero.Number).IsEqualTo(200);
        }

        [global::TUnit.Core.Test]
        public async Task DateFormatsUtcTimestampWhenPrefixedWithBang()
        {
            Script script = CreateScript();
            DynValue formatted = script.DoString(
                "return os.date('!%Y-%m-%d %H:%M:%S', 1609459200)"
            );

            await Assert.That(formatted.String).IsEqualTo("2021-01-01 00:00:00");
        }

        [global::TUnit.Core.Test]
        public async Task DateReturnsTableWhenRequested()
        {
            Script script = CreateScript();
            DynValue tableValue = script.DoString("return os.date('!*t', 1609459200)");

            await Assert.That(tableValue.Type).IsEqualTo(DataType.Table);
            await Assert.That(tableValue.Table.Get("year").Number).IsEqualTo(2021);
            await Assert.That(tableValue.Table.Get("month").Number).IsEqualTo(1);
            await Assert.That(tableValue.Table.Get("day").Number).IsEqualTo(1);
            await Assert.That(tableValue.Table.Get("isdst").Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task DateReturnsLocalTableWhenPrefixOmitted()
        {
            DateTime localTime = DateTimeOffset.FromUnixTimeSeconds(1609459200).LocalDateTime;
            Script script = CreateScript();
            DynValue tableValue = script.DoString("return os.date('*t', 1609459200)");

            await Assert.That(tableValue.Type).IsEqualTo(DataType.Table);
            await Assert.That(tableValue.Table.Get("year").Number).IsEqualTo(localTime.Year);
            await Assert.That(tableValue.Table.Get("month").Number).IsEqualTo(localTime.Month);
            await Assert.That(tableValue.Table.Get("day").Number).IsEqualTo(localTime.Day);
            await Assert.That(tableValue.Table.Get("hour").Number).IsEqualTo(localTime.Hour);
            await Assert.That(tableValue.Table.Get("min").Number).IsEqualTo(localTime.Minute);
            await Assert.That(tableValue.Table.Get("sec").Number).IsEqualTo(localTime.Second);
            await Assert
                .That(tableValue.Table.Get("isdst").Boolean)
                .IsEqualTo(localTime.IsDaylightSavingTime());
        }

        [global::TUnit.Core.Test]
        public async Task DateFormatsWeekPatterns()
        {
            Script script = CreateScript();
            DynValue epochWeeks = script.DoString("return os.date('!%U-%W-%V', 0)");
            DynValue marchWeeks = script.DoString("return os.date('!%U-%W-%V', 345600)");

            await Assert.That(epochWeeks.String).IsEqualTo("00-00-01");
            await Assert.That(marchWeeks.String).IsEqualTo("01-01-02");
        }

        [global::TUnit.Core.Test]
        public async Task DateIgnoresOAndEFormatModifiers()
        {
            Script script = CreateScript();
            DynValue formatted = script.DoString("return os.date('!%OY-%Ew', 0)");

            await Assert.That(formatted.String).IsEqualTo("1970-4");
        }

        [global::TUnit.Core.Test]
        public async Task DateSupportsOyModifier()
        {
            Script script = CreateScript();
            DynValue formatted = script.DoString("return os.date('!%Oy', 0)");

            await Assert.That(formatted.String).IsEqualTo("70");
        }

        [global::TUnit.Core.Test]
        public async Task DateSupportsEscapeAndExtendedSpecifiers()
        {
            Script script = CreateScript();
            DynValue formatted = script.DoString(
                "return os.date('!%e|%n|%t|%%|%C|%j|%u|%w', 1609459200)"
            );

            await Assert.That(formatted.String).IsEqualTo(" 1|\n|\t|%|20|001|5|5");
        }

        [global::TUnit.Core.Test]
        public async Task DateThrowsWhenConversionSpecifierUnknown()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.DoString("return os.date('%Q', 1609459200)")
            );

            await Assert.That(exception.Message).Contains("invalid conversion specifier");
        }

        [global::TUnit.Core.Test]
        public async Task TimeReturnsCurrentProviderTimestampWhenNoArguments()
        {
            long unixSeconds = 1_234_567;
            DateTimeOffset stamp = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            Script script = CreateScriptWithTimeProvider(stamp, stamp);

            DynValue result = script.DoString("return os.time()");

            await Assert.That(result.Number).IsEqualTo(unixSeconds);
        }

        [global::TUnit.Core.Test]
        public async Task TimeDefaultsHourToNoonWhenFieldsOmitted()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "return os.time({ year = 1970, month = 1, day = 1 })"
            );

            await Assert.That(result.Number).IsEqualTo(12 * 60 * 60);
        }

        [global::TUnit.Core.Test]
        public async Task TimeIgnoresNonNumericOptionalFields()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "return os.time({ year = 1970, month = 1, day = 1, hour = 'ignored' })"
            );

            await Assert.That(result.Number).IsEqualTo(12 * 60 * 60);
        }

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }

        private static Script CreateScriptWithTimeProvider(params DateTimeOffset[] timestamps)
        {
            if (timestamps == null || timestamps.Length == 0)
            {
                throw new ArgumentException(
                    "At least one timestamp must be provided.",
                    nameof(timestamps)
                );
            }

            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                TimeProvider = new SequenceTimeProvider(timestamps),
            };

            return new Script(CoreModules.PresetComplete, options);
        }

        private static TException ExpectException<TException>(Func<DynValue> action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }

        private sealed class SequenceTimeProvider : ITimeProvider
        {
            private readonly Queue<DateTimeOffset> _values;
            private DateTimeOffset _last;

            internal SequenceTimeProvider(params DateTimeOffset[] values)
            {
                _values = new Queue<DateTimeOffset>(values);
                _last = values[^1];
            }

            public DateTimeOffset GetUtcNow()
            {
                if (_values.Count > 0)
                {
                    _last = _values.Dequeue();
                }

                return _last;
            }
        }
    }
}
