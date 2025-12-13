namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;

    public sealed class BasicModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TypeThrowsWhenArgumentsAreNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                BasicModule.Type(null, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task TypeThrowsWhenNoArgumentsProvided()
        {
            CallbackArguments args = new(Array.Empty<DynValue>(), isMethodCall: false);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.Type(null, args)
            );

            await Assert.That(exception.Message).Contains("type");
        }

        [global::TUnit.Core.Test]
        public async Task CollectGarbageThrowsWhenArgumentsAreNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                BasicModule.CollectGarbage(null, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task CollectGarbageRunsWhenModeIsCollect()
        {
            CallbackArguments args = new(new[] { DynValue.Nil }, isMethodCall: false);

            DynValue result = BasicModule.CollectGarbage(null, args);

            await Assert.That(result).IsEqualTo(DynValue.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task CollectGarbageSkipsWhenModeIsNotSupported()
        {
            CallbackArguments args = new(new[] { DynValue.NewString("stop") }, isMethodCall: false);

            DynValue result = BasicModule.CollectGarbage(null, args);

            await Assert.That(result).IsEqualTo(DynValue.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task ToStringContinuationThrowsWhenMetamethodReturnsNonString()
        {
            CallbackArguments args = new(new[] { DynValue.NewNumber(5) }, isMethodCall: false);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.ToStringContinuation(null, args)
            );

            await Assert.That(exception.Message).Contains("tostring");
        }

        [global::TUnit.Core.Test]
        public async Task SelectCountsTupleArgumentsWhenHashRequested()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2));
            CallbackArguments args = new(
                new[] { DynValue.NewString("#"), DynValue.NewNumber(10), tuple },
                false
            );

            DynValue result = BasicModule.Select(null, args);

            await Assert.That(result.Number).IsEqualTo(3d);
        }

        [global::TUnit.Core.Test]
        public async Task WarnThrowsWhenExecutionContextIsNull()
        {
            CallbackArguments args = new(new[] { DynValue.NewString("payload") }, false);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                BasicModule.Warn(null, args)
            );

            await Assert.That(exception.ParamName).IsEqualTo("executionContext");
        }

        [global::TUnit.Core.Test]
        public async Task WarnInvokesCustomWarnHandler()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            string observed = null;
            script.Globals.Set(
                "_WARN",
                DynValue.NewCallback(
                    (_, warnArgs) =>
                    {
                        observed = warnArgs[0].String;
                        return DynValue.Nil;
                    }
                )
            );

            CallbackArguments args = new(new[] { DynValue.NewString("custom-warning") }, false);
            BasicModule.Warn(context, args);

            await Assert.That(observed).IsEqualTo("custom-warning");
        }

        [global::TUnit.Core.Test]
        public async Task WarnUsesDebugPrintWhenHandlerMissing()
        {
            Script script = new();
            script.Globals.Set("_WARN", DynValue.Nil);
            string observed = null;
            script.Options.DebugPrint = s => observed = s;
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            CallbackArguments args = new(new[] { DynValue.NewString("debug-warning") }, false);
            BasicModule.Warn(context, args);

            await Assert.That(observed).IsEqualTo("debug-warning");
        }

        [global::TUnit.Core.Test]
        public async Task WarnWritesToConsoleWhenNoHandlerOrDebugPrint()
        {
            Script script = new();
            script.Globals.Set("_WARN", DynValue.Nil);
            script.Options.DebugPrint = null;
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            string output = string.Empty;
            await ConsoleTestUtilities
                .WithConsoleCaptureAsync(
                    consoleScope =>
                    {
                        CallbackArguments args = new(
                            new[] { DynValue.NewString("console-warning") },
                            false
                        );
                        BasicModule.Warn(context, args);
                        output = consoleScope.Writer.ToString();
                        return Task.CompletedTask;
                    },
                    captureError: true
                )
                .ConfigureAwait(false);

            await Assert.That(output).Contains("console-warning");
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberReturnsNilWhenInvalidDigitProvidedForBase()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("17"), DynValue.NewNumber(6) },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberThrowsWhenBaseIsNaN()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("FF"), DynValue.NewNumber(double.NaN) },
                isMethodCall: false
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.ToNumber(context, args)
            );

            await Assert.That(exception.Message).Contains("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberThrowsWhenBaseIsPositiveInfinity()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("FF"), DynValue.NewNumber(double.PositiveInfinity) },
                isMethodCall: false
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.ToNumber(context, args)
            );

            await Assert.That(exception.Message).Contains("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberThrowsWhenBaseIsNegativeInfinity()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("FF"), DynValue.NewNumber(double.NegativeInfinity) },
                isMethodCall: false
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.ToNumber(context, args)
            );

            await Assert.That(exception.Message).Contains("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberThrowsWhenBaseIsNotInteger()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("FF"), DynValue.NewNumber(16.5) },
                isMethodCall: false
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.ToNumber(context, args)
            );

            await Assert.That(exception.Message).Contains("integer").ConfigureAwait(false);
        }

        // ========================================
        // Hex String Parsing Tests (Lua §3.1 / §6.1)
        // tonumber without base should parse hex strings with 0x/0X prefix
        // ========================================

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexStringWithoutBase()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { DynValue.NewString("0xFF") }, isMethodCall: false);

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(255d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesLowercaseHexPrefixWithoutBase()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { DynValue.NewString("0x1a") }, isMethodCall: false);

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(26d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesUppercaseHexPrefixWithoutBase()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { DynValue.NewString("0X1A") }, isMethodCall: false);

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(26d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesNegativeHexStringWithoutBase()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("-0x10") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(-16d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesPositiveHexStringWithPlusSign()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("+0x10") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(16d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexStringWithWhitespace()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("  0xFF  ") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(255d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberReturnsNilForInvalidHexString()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            // "0x" without digits is invalid
            CallbackArguments args = new(new[] { DynValue.NewString("0x") }, isMethodCall: false);

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberReturnsNilForHexStringWithInvalidChars()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            // "0xG" contains invalid hex digit
            CallbackArguments args = new(new[] { DynValue.NewString("0xG") }, isMethodCall: false);

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesLargeHexStringWithoutBase()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("0xDeAdBeEf") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(3735928559d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexFloatWithFraction()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            // 0x1.8 = 1 + 8/16 = 1.5, p0 means * 2^0 = 1.5
            CallbackArguments args = new(
                new[] { DynValue.NewString("0x1.8p0") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(1.5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexFloatWithExponent()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            // 0x1p2 = 1 * 2^2 = 4
            CallbackArguments args = new(
                new[] { DynValue.NewString("0x1p2") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexFloatWithNegativeExponent()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            // 0x10p-2 = 16 * 2^(-2) = 16 / 4 = 4
            CallbackArguments args = new(
                new[] { DynValue.NewString("0x10p-2") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }

        // ========================================
        // Version-Specific Hex Parsing Tests (Lua §3.1)
        // Lua 5.1 does NOT support hex string parsing in tonumber without explicit base.
        // Hex parsing (0x prefix) was added in Lua 5.2.
        // ========================================

        [global::TUnit.Core.Test]
        public async Task ToNumberReturnsNilForHexStringInLua51()
        {
            // In Lua 5.1, tonumber('0xFF') without a base should return nil
            Script script = CreateScript(LuaCompatibilityVersion.Lua51);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { DynValue.NewString("0xFF") }, isMethodCall: false);

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexStringInLua52Plus(LuaCompatibilityVersion version)
        {
            // In Lua 5.2+, tonumber('0xFF') without a base should parse the hex string
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { DynValue.NewString("0xFF") }, isMethodCall: false);

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(255d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberReturnsNilForNegativeHexStringInLua51()
        {
            // In Lua 5.1, tonumber('-0x10') without a base should return nil
            Script script = CreateScript(LuaCompatibilityVersion.Lua51);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("-0x10") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberReturnsNilForHexFloatInLua51()
        {
            // In Lua 5.1, tonumber('0x1.8p0') without a base should return nil
            Script script = CreateScript(LuaCompatibilityVersion.Lua51);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("0x1.8p0") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesLargeHexIntegerWithFullPrecision()
        {
            // Test that large hex integers are parsed with full 64-bit precision
            // 0x7FFFFFFFFFFFFFFF = long.MaxValue = 9223372036854775807
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("0x7FFFFFFFFFFFFFFF") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            // Should be stored as integer with exact value
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(9223372036854775807L)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexIntegerWithValueNearMaxLong()
        {
            // 0x123456789ABCDEF = 81985529216486895 (within long range)
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("0x123456789ABCDEF") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(81985529216486895L)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexIntegerExceedingLongAsFloat()
        {
            // 0xFFFFFFFFFFFFFFFF = 18446744073709551615 (exceeds long.MaxValue)
            // Should be parsed as float since it can't fit in a signed 64-bit integer
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("0xFFFFFFFFFFFFFFFF") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
            // Due to double precision limits, the value will be approximately 1.844674407370955e+19
            await Assert.That(result.LuaNumber.AsFloat).IsGreaterThan(1.8e19).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesNegativeMaxLongCorrectly()
        {
            // -0x8000000000000000 = long.MinValue = -9223372036854775808
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("-0x8000000000000000") },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(long.MinValue)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelectErrorsOnNonIntegerIndexLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            // select(1.5, 'a', 'b') should error in Lua 5.3+
            await Assert
                .That(() => script.DoString("return select(1.5, 'a', 'b', 'c')"))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task SelectTruncatesNonIntegerIndexLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            // select(1.5, 'a', 'b', 'c') should truncate to 1 and return all elements
            DynValue result = script.DoString("return select(1.5, 'a', 'b', 'c')");

            // 1.5 floors to 1, so returns all 3 arguments
            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelectAcceptsIntegralFloatLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            // select(2.0, 'a', 'b', 'c') should work since 2.0 has integer representation
            DynValue result = script.DoString("return select(2.0, 'a', 'b', 'c')");

            // 2.0 is treated as integer 2, so returns 'b' and 'c'
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].String).IsEqualTo("b").ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("c").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ErrorLevelErrorsOnNonIntegerLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            // error('msg', 1.5) should error about level in Lua 5.3+
            await Assert
                .That(() => script.DoString("error('test', 1.5)"))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task ErrorLevelTruncatesNonIntegerLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            // error('msg', 1.5) should truncate level to 1 and throw the error message
            await Assert
                .That(() => script.DoString("error('test message', 1.5)"))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        // print() Version-Specific Behavior Tests

        /// <summary>
        /// In Lua 5.1-5.3, print() calls the global tostring function, which can be overridden.
        /// This test verifies that overriding the global tostring affects print() output.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task PrintCallsGlobalTostringInLua51To53(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            string output = null;
            script.Options.DebugPrint = s => output = s;

            // Override global tostring to return a custom prefix
            script.DoString(
                @"
                function tostring(v)
                    return 'CUSTOM:' .. type(v)
                end
                t = setmetatable({}, { __tostring = function() return 'META' end })
                print(t)
            "
            );

            // In Lua 5.1-5.3, print calls global tostring, so we get 'CUSTOM:table' not 'META'
            await Assert.That(output).IsEqualTo("CUSTOM:table").ConfigureAwait(false);
        }

        /// <summary>
        /// In Lua 5.4+, print() uses the __tostring metamethod directly (hardwired behavior),
        /// bypassing the global tostring function even if it's overridden.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PrintUsesTostringMetamethodDirectlyInLua54Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            string output = null;
            script.Options.DebugPrint = s => output = s;

            // Override global tostring - should be ignored in Lua 5.4+
            script.DoString(
                @"
                function tostring(v)
                    return 'CUSTOM:' .. type(v)
                end
                t = setmetatable({}, { __tostring = function() return 'META' end })
                print(t)
            "
            );

            // In Lua 5.4+, print uses __tostring directly, so we get 'META' not 'CUSTOM:table'
            await Assert.That(output).IsEqualTo("META").ConfigureAwait(false);
        }

        /// <summary>
        /// In Lua 5.4+, when there's no __tostring metamethod but global tostring is overridden,
        /// print() should still use default formatting (not call the overridden global tostring).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PrintIgnoresGlobalTostringForPlainTablesInLua54Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            string output = null;
            script.Options.DebugPrint = s => output = s;

            // Override global tostring but use a plain table without __tostring
            script.DoString(
                @"
                function tostring(v)
                    return 'CUSTOM:' .. type(v)
                end
                t = {}  -- plain table, no metatable
                print(t)
            "
            );

            // In Lua 5.4+, print uses default formatting for tables without __tostring
            // Should print something like "table: 0x..." not "CUSTOM:table"
            await Assert.That(output).Contains("table:").ConfigureAwait(false);
            await Assert.That(output).DoesNotContain("CUSTOM").ConfigureAwait(false);
        }

        /// <summary>
        /// In Lua 5.1-5.3, when there's no __tostring metamethod but global tostring is overridden,
        /// print() should call the overridden global tostring.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task PrintCallsGlobalTostringForPlainTablesInLua51To53(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            string output = null;
            script.Options.DebugPrint = s => output = s;

            // Override global tostring and use a plain table without __tostring
            script.DoString(
                @"
                function tostring(v)
                    return 'CUSTOM:' .. type(v)
                end
                t = {}  -- plain table, no metatable
                print(t)
            "
            );

            // In Lua 5.1-5.3, print calls global tostring, so we get 'CUSTOM:table'
            await Assert.That(output).IsEqualTo("CUSTOM:table").ConfigureAwait(false);
        }

        /// <summary>
        /// In Lua 5.1-5.3, print() uses the global tostring even for primitive types like numbers.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task PrintCallsGlobalTostringForNumbersInLua51To53(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            string output = null;
            script.Options.DebugPrint = s => output = s;

            // Override global tostring to format numbers specially
            script.DoString(
                @"
                function tostring(v)
                    if type(v) == 'number' then
                        return 'NUM:' .. v
                    end
                    return v
                end
                print(42)
            "
            );

            // In Lua 5.1-5.3, print calls global tostring
            await Assert.That(output).IsEqualTo("NUM:42").ConfigureAwait(false);
        }

        /// <summary>
        /// In Lua 5.4+, print() uses default formatting for primitive types,
        /// ignoring any global tostring override.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PrintIgnoresGlobalTostringForNumbersInLua54Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            string output = null;
            script.Options.DebugPrint = s => output = s;

            // Override global tostring - should be ignored in Lua 5.4+
            script.DoString(
                @"
                function tostring(v)
                    if type(v) == 'number' then
                        return 'NUM:' .. v
                    end
                    return v
                end
                print(42)
            "
            );

            // In Lua 5.4+, print uses default formatting, not global tostring
            await Assert.That(output).IsEqualTo("42").ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that print() with multiple arguments separates them with tabs,
        /// regardless of Lua version.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        public async Task PrintSeparatesArgumentsWithTabs(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            string output = null;
            script.Options.DebugPrint = s => output = s;

            script.DoString("print(1, 2, 3)");

            await Assert.That(output).IsEqualTo("1\t2\t3").ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that print() with a ClrFunction tostring replacement works in Lua 5.1-5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task PrintWorksWithClrFunctionTostringInLua51To53(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            string output = null;
            script.Options.DebugPrint = s => output = s;

            // Replace global tostring with a CLR callback
            script.Globals["tostring"] = DynValue.NewCallback(
                (_, args) =>
                {
                    return DynValue.NewString("CLR:" + args[0].Type);
                }
            );

            script.DoString("print({})");

            // CLR tostring should be called
            await Assert.That(output).IsEqualTo("CLR:Table").ConfigureAwait(false);
        }

        private static Script CreateScript(
            LuaCompatibilityVersion version = LuaCompatibilityVersion.Lua54
        )
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            return new Script(CoreModulePresets.Complete, options);
        }
    }
}
