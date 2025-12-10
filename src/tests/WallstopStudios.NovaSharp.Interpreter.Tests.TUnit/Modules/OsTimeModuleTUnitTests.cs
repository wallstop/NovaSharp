namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
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
        public async Task TimeReturnsNegativeForDatesBeforeEpoch()
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

            // Dates before Unix epoch (Jan 1, 1970) return negative values
            // December 31, 1969 23:59:59 UTC is -1 second from epoch
            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsLessThan(0);
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
        public async Task DiffTimeHandlesTwoArguments()
        {
            Script script = CreateScript();
            DynValue diff = script.DoString("return os.difftime(200, 150)");

            await Assert.That(diff.Number).IsEqualTo(50);
        }

        [global::TUnit.Core.Test]
        public async Task DiffTimeOptionalSecondArgumentInLua52()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua52);
            DynValue diffFromZero = script.DoString("return os.difftime(200)");

            // In Lua 5.1/5.2, second argument is optional and defaults to 0
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
            // NOTE: This documents a NovaSharp extension. Standard Lua rejects %O and %E
            // format modifiers as invalid conversion specifiers. NovaSharp strips them and
            // treats the following character as the format specifier (POSIX-style behavior).
            // For example, %OY becomes %Y (year) and %Ew becomes %w (weekday).
            // This is a known divergence from standard Lua behavior.
            Script script = CreateScript();
            DynValue formatted = script.DoString("return os.date('!%OY-%Ew', 0)");

            await Assert.That(formatted.String).IsEqualTo("1970-4");
        }

        // This is a documentation comment explaining the known spec divergence.
        // No test assertion needed - the DateIgnoresOAndEFormatModifiers test above
        // already validates NovaSharp's behavior, and this comment documents
        // that it differs from standard Lua.
        //
        // Standard Lua behavior:
        //   lua5.2 -e "print(os.date('%OY-%Ew', 0))"
        //   lua5.2: (command line):1: bad argument #1 to 'date' (invalid conversion specifier '%OY-%Ew')
        //
        // NovaSharp behavior:
        //   NovaSharp accepts %O and %E as POSIX-style modifier prefixes and strips them.
        //   This results in %OY -> %Y (year: 1970) and %Ew -> %w (weekday: 4 for Thursday)
        //   Output: "1970-4"
        //
        // If we want to match standard Lua behavior, we would need to add a check
        // in OsTimeModule.cs to reject %O and %E as unknown specifiers.

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

        // Lua 5.2+ throws for unknown conversion specifiers
        [global::TUnit.Core.Test]
        public async Task DateThrowsWhenConversionSpecifierUnknownInLua52Plus()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua52);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.DoString("return os.date('%Q', 1609459200)")
            );

            await Assert.That(exception.Message).Contains("invalid conversion specifier");
        }

        // Lua 5.1 returns the literal specifier for unknown conversion specifiers
        [global::TUnit.Core.Test]
        public async Task DateReturnsLiteralForUnknownSpecifierInLua51()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua51);

            DynValue result = script.DoString("return os.date('%Q', 1609459200)");

            await Assert.That(result.String).Contains("%Q").ConfigureAwait(false);
        }

        // Data-driven tests for invalid specifier error messages in Lua 5.2+
        // These verify that the error message correctly includes the trailing context character
        [global::TUnit.Core.Test]
        [Arguments("%Ja", "Ja", "Invalid specifier with trailing char 'a'")] // Specifier J with trailing 'a'
        [Arguments("%Qb", "Qb", "Invalid specifier Q with trailing char 'b'")] // Specifier Q with trailing 'b'
        [Arguments("%qx", "qx", "Invalid specifier q with trailing char 'x'")] // Specifier q with trailing 'x'
        [Arguments("%J", "J", "Invalid specifier J at end of string")] // Specifier at end - no trailing char
        [Arguments("%Q", "Q", "Invalid specifier Q at end of string")] // Specifier at end - no trailing char
        [Arguments("hello %Ja world", "Ja", "Invalid specifier in middle of format string")]
        [Arguments("%Y-%J-test", "J-", "Invalid specifier between valid specifiers")] // J with '-' trailing
        public async Task DateErrorMessageIncludesCorrectSpecifierContext(
            string formatString,
            string expectedSpecifier,
            string scenarioDescription
        )
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua52);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
            {
                DynValue result = script.DoString($"return os.date('{formatString}', 1609459200)");
                // If we get here, no exception was thrown
                throw new InvalidOperationException(
                    $"Expected ScriptRuntimeException but got result: {result?.ToString() ?? "null"}. Scenario: {scenarioDescription}"
                );
            });

            string expectedPattern = $"'%{expectedSpecifier}'";
            await Assert
                .That(exception.Message)
                .Contains(expectedPattern)
                .Because(
                    $"{scenarioDescription}: Expected error message to contain '{expectedPattern}' but got: {exception.Message}"
                )
                .ConfigureAwait(false);

            // Also verify the standard parts of the message are present
            await Assert
                .That(exception.Message)
                .Contains("bad argument #1 to 'date'")
                .ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("invalid conversion specifier")
                .ConfigureAwait(false);
        }

        // Data-driven tests for Lua 5.1 literal output (unknown specifiers pass through)
        [global::TUnit.Core.Test]
        [Arguments("%Ja", "%J", "Specifier J with trailing char - outputs literal %J")]
        [Arguments("%Qb", "%Q", "Specifier Q with trailing char - outputs literal %Q")]
        [Arguments("%J", "%J", "Specifier J at end - outputs literal %J")]
        [Arguments("hello %Q world", "hello %Q world", "Specifier in middle - preserves context")]
        public async Task DateLua51OutputsLiteralForUnknownSpecifiers(
            string formatString,
            string expectedOutput,
            string scenarioDescription
        )
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua51);

            DynValue result = script.DoString($"return os.date('{formatString}', 1609459200)");

            await Assert
                .That(result.String)
                .Contains(expectedOutput)
                .Because(
                    $"{scenarioDescription}: Expected output to contain '{expectedOutput}' but got: {result.String}"
                )
                .ConfigureAwait(false);
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
        public async Task TimeIgnoresNonNumericOptionalFieldsInLua52()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua52);
            DynValue result = script.DoString(
                "return os.time({ year = 1970, month = 1, day = 1, hour = 'ignored' })"
            );

            // In Lua 5.1/5.2, non-numeric strings in optional fields are ignored,
            // and the default value (12 for hour) is used
            await Assert.That(result.Number).IsEqualTo(12 * 60 * 60);
        }

        [global::TUnit.Core.Test]
        public async Task TimeThrowsForNonNumericFieldsInLua53()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua53);

            ScriptRuntimeException exception = await Assert
                .ThrowsAsync<ScriptRuntimeException>(() =>
                    Task.FromResult(
                        script.DoString(
                            "return os.time({ year = 1970, month = 1, day = 1, hour = 'ignored' })"
                        )
                    )
                )
                .ConfigureAwait(false);

            await Assert
                .That(exception.Message)
                .Contains("field 'hour' is not an integer")
                .ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }

        private static Script CreateScriptWithVersion(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            return new Script(CoreModules.PresetComplete, options);
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
