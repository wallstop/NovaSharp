namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;

    public sealed class BasicModuleTUnitTests
    {
        private static readonly OpCode[] SingleByteOpCodes = new OpCode[256];
        private static readonly OpCode[] MultiByteOpCodes = new OpCode[256];

        static BasicModuleTUnitTests()
        {
            FieldInfo[] fields = typeof(OpCodes).GetFields(
                BindingFlags.Public | BindingFlags.Static
            );
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].GetValue(null) is not OpCode opCode)
                {
                    continue;
                }

                ushort value = unchecked((ushort)opCode.Value);
                if (value < 0x100)
                {
                    SingleByteOpCodes[value] = opCode;
                }
                else if ((value & 0xff00) == 0xfe00)
                {
                    MultiByteOpCodes[value & 0xff] = opCode;
                }
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TypeThrowsWhenArgumentsAreNull(LuaCompatibilityVersion version)
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                BasicModule.Type(null, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ToStringMetamethodTailRequestsReuseContinuation(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue value = script.DoString(
                "return setmetatable({}, { __tostring = function() return 'value' end })"
            );
            CallbackArguments args = new(new[] { value }, isMethodCall: false);

            LuaValue first = BasicModule.ToString(context, args);
            LuaValue second = BasicModule.ToString(context, args);
            second.TailCallData.Continuation.AdditionalData = "dirty";
            LuaValue third = BasicModule.ToString(context, args);

            await Assert.That(first.Type).IsEqualTo(DataType.TailCallRequest).ConfigureAwait(false);
            await Assert
                .That(second.Type)
                .IsEqualTo(DataType.TailCallRequest)
                .ConfigureAwait(false);
            await Assert
                .That(first.TailCallData.Continuation)
                .IsSameReferenceAs(second.TailCallData.Continuation)
                .ConfigureAwait(false);
            await Assert
                .That(first.TailCallData.Continuation)
                .IsSameReferenceAs(third.TailCallData.Continuation)
                .ConfigureAwait(false);
            await Assert
                .That(first.TailCallData.Continuation.Name)
                .IsEqualTo(Metamethods.ToStringMeta)
                .ConfigureAwait(false);
            await Assert
                .That(third.TailCallData.Continuation.AdditionalData)
                .IsNull()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToStringMetamethodTailRequestsUseThreadLocalContinuation()
        {
            CallbackFunction main = CreateToStringContinuationOnCurrentThread();
            CallbackFunction worker = RunOnNewThread(CreateToStringContinuationOnCurrentThread);

            await Assert.That(main).IsNotSameReferenceAs(worker).ConfigureAwait(false);
            await Assert
                .That(worker.Name)
                .IsEqualTo(Metamethods.ToStringMeta)
                .ConfigureAwait(false);
            await Assert.That(worker.AdditionalData).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TypeThrowsWhenNoArgumentsProvided(LuaCompatibilityVersion version)
        {
            CallbackArguments args = new(Array.Empty<LuaValue>(), isMethodCall: false);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.Type(null, args)
            );

            await Assert.That(exception.Message).Contains("type");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CollectGarbageThrowsWhenArgumentsAreNull(LuaCompatibilityVersion version)
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                BasicModule.CollectGarbage(null, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CollectGarbageRunsWhenModeIsCollect(LuaCompatibilityVersion version)
        {
            CallbackArguments args = new(new[] { LuaValue.Nil }, isMethodCall: false);

            LuaValue result = BasicModule.CollectGarbage(null, args);

            await Assert.That(result).IsEqualTo(LuaValue.Nil);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CollectGarbageSkipsWhenModeIsNotSupported(LuaCompatibilityVersion version)
        {
            CallbackArguments args = new(new[] { LuaValue.NewString("stop") }, isMethodCall: false);

            LuaValue result = BasicModule.CollectGarbage(null, args);

            await Assert.That(result).IsEqualTo(LuaValue.Nil);
        }

        /// <summary>
        /// Verifies that <see cref="BasicModule.ToStringContinuation"/> throws when executionContext is null.
        /// This is a defensive programming check - the continuation cannot execute without a valid context.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task ToStringContinuationThrowsWhenExecutionContextIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                BasicModule.ToStringContinuation(null, default)
            );

            await Assert.That(exception.ParamName).IsEqualTo("executionContext");
        }

        /// <summary>
        /// Verifies that <see cref="BasicModule.ToStringContinuation"/> treats missing results as nil,
        /// which raises the Lua 5.3+ string requirement and passes through on earlier versions.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ToStringContinuationHandlesMissingResult(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            ScriptExecutionContext executionContext = script.CreateDynamicExecutionContext();

            if (version >= LuaCompatibilityVersion.Lua53)
            {
                ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                    BasicModule.ToStringContinuation(executionContext, default)
                );

                await Assert
                    .That(exception.Message)
                    .Contains("'__tostring' must return a string")
                    .ConfigureAwait(false);
                return;
            }

            LuaValue result = BasicModule.ToStringContinuation(executionContext, default);

            await Assert.That(result.IsNil).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelectCountsTupleArgumentsWhenHashRequested(
            LuaCompatibilityVersion version
        )
        {
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewNumber(2));
            CallbackArguments args = new(
                new[] { LuaValue.NewString("#"), LuaValue.NewNumber(10), tuple },
                false
            );

            LuaValue result = BasicModule.Select(null, args);

            await Assert.That(result.Number).IsEqualTo(3d);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WarnThrowsWhenExecutionContextIsNull(LuaCompatibilityVersion version)
        {
            CallbackArguments args = new(new[] { LuaValue.NewString("payload") }, false);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                BasicModule.Warn(null, args)
            );

            await Assert.That(exception.ParamName).IsEqualTo("executionContext");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WarnInvokesCustomWarnHandler(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            string observed = null;
            script.Globals.Set(
                "_WARN",
                LuaValue.NewCallback(
                    (_, warnArgs) =>
                    {
                        observed = warnArgs[0].String;
                        return LuaValue.Nil;
                    }
                )
            );

            script.DoString("warn('@on'); warn('custom-', 7)");

            await Assert.That(observed).IsEqualTo("custom-7");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WarnUsesConfiguredStderrWhenHandlerMissing(
            LuaCompatibilityVersion version
        )
        {
            using MemoryStream stderr = new();
            ScriptOptions options = new(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
                Stderr = stderr,
            };
            Script script = new(CoreModulePresets.Complete, options);

            script.DoString("warn('@on'); warn('stream-', 8)");

            string observed = Encoding.UTF8.GetString(stderr.ToArray());
            await Assert.That(observed).IsEqualTo("Lua warning: stream-8" + Environment.NewLine);
            await Assert.That(stderr.CanWrite).IsTrue();
            long lengthBeforeOwnershipProbe = stderr.Length;
            stderr.WriteByte((byte)'!');
            await Assert.That(stderr.Length).IsEqualTo(lengthBeforeOwnershipProbe + 1);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WarnStateIsIsolatedAcrossScripts(LuaCompatibilityVersion version)
        {
            Script firstScript = CreateScript(version);
            Script secondScript = CreateScript(version);
            ScriptExecutionContext firstContext = firstScript.CreateDynamicExecutionContext();
            ScriptExecutionContext secondContext = secondScript.CreateDynamicExecutionContext();
            List<string> firstObserved = new();
            List<string> secondObserved = new();
            firstScript.Globals.Set(
                "_WARN",
                LuaValue.NewCallback(
                    (_, warnArgs) =>
                    {
                        firstObserved.Add(warnArgs[0].String);
                        return LuaValue.Nil;
                    }
                )
            );
            secondScript.Globals.Set(
                "_WARN",
                LuaValue.NewCallback(
                    (_, warnArgs) =>
                    {
                        secondObserved.Add(warnArgs[0].String);
                        return LuaValue.Nil;
                    }
                )
            );

            BasicModule.Warn(
                firstContext,
                new CallbackArguments(new[] { LuaValue.NewString("@on") }, false)
            );
            BasicModule.Warn(
                firstContext,
                new CallbackArguments(new[] { LuaValue.NewString("first") }, false)
            );
            BasicModule.Warn(
                secondContext,
                new CallbackArguments(new[] { LuaValue.NewString("second-disabled") }, false)
            );
            BasicModule.Warn(
                secondContext,
                new CallbackArguments(new[] { LuaValue.NewString("@on") }, false)
            );
            BasicModule.Warn(
                secondContext,
                new CallbackArguments(new[] { LuaValue.NewString("second") }, false)
            );
            BasicModule.Warn(
                firstContext,
                new CallbackArguments(new[] { LuaValue.NewString("first-again") }, false)
            );

            await Assert.That(firstObserved.Count).IsEqualTo(2);
            await Assert.That(firstObserved[0]).IsEqualTo("first");
            await Assert.That(firstObserved[1]).IsEqualTo("first-again");
            await Assert.That(secondObserved.Count).IsEqualTo(1);
            await Assert.That(secondObserved[0]).IsEqualTo("second");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.NotInParallel]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WarnWritesToConsoleWhenNoHandlerOrConfiguredStderr(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            script.Globals.Set("_WARN", LuaValue.Nil);

            string output = string.Empty;
            await ConsoleTestUtilities
                .WithConsoleCaptureAsync(
                    consoleScope =>
                    {
                        script.DoString("warn('@on'); warn('console-warning')");
                        output = consoleScope.Writer.ToString();
                        return Task.CompletedTask;
                    },
                    captureError: true
                )
                .ConfigureAwait(false);

            await Assert
                .That(output)
                .IsEqualTo("Lua warning: console-warning" + Environment.NewLine);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WarnDefaultsOffAndHonorsControlMessages(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            List<string> observed = new();
            script.Globals.Set(
                "_WARN",
                LuaValue.NewCallback(
                    (_, warnArgs) =>
                    {
                        observed.Add(warnArgs[0].String);
                        return LuaValue.Nil;
                    }
                )
            );

            script.DoString(
                @"
warn('disabled')
warn('@unknown')
warn('@on')
warn('enabled-', 9)
warn('@unknown')
warn('@off', '-is-data')
warn('@off')
warn('disabled-again')
"
            );

            await Assert.That(observed.Count).IsEqualTo(2);
            await Assert.That(observed[0]).IsEqualTo("enabled-9");
            await Assert.That(observed[1]).IsEqualTo("@off-is-data");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WarnValidatesEveryArgumentWhileDisabled(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException missing = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("warn()")
            );
            ScriptRuntimeException invalidSecond = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("warn('valid', true)")
            );

            await Assert.That(missing.Message).Contains("bad argument #1");
            await Assert.That(missing.Message).Contains("string expected");
            await Assert.That(invalidSecond.Message).Contains("bad argument #2");
            await Assert.That(invalidSecond.Message).Contains("string expected");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberReturnsNilWhenInvalidDigitProvidedForBase(
            LuaCompatibilityVersion version
        )
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("17"), LuaValue.NewNumber(6) },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.IsNil).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberThrowsWhenBaseIsNaN(LuaCompatibilityVersion version)
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("FF"), LuaValue.NewNumber(double.NaN) },
                isMethodCall: false
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.ToNumber(context, args)
            );

            await Assert.That(exception.Message).Contains("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberThrowsWhenBaseIsPositiveInfinity(LuaCompatibilityVersion version)
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("FF"), LuaValue.NewNumber(double.PositiveInfinity) },
                isMethodCall: false
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.ToNumber(context, args)
            );

            await Assert.That(exception.Message).Contains("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberThrowsWhenBaseIsNegativeInfinity(LuaCompatibilityVersion version)
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("FF"), LuaValue.NewNumber(double.NegativeInfinity) },
                isMethodCall: false
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.ToNumber(context, args)
            );

            await Assert.That(exception.Message).Contains("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberThrowsWhenBaseIsNotInteger(LuaCompatibilityVersion version)
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("FF"), LuaValue.NewNumber(16.5) },
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexStringWithoutBase(LuaCompatibilityVersion version)
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { LuaValue.NewString("0xFF") }, isMethodCall: false);

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(255d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesLowercaseHexPrefixWithoutBase(
            LuaCompatibilityVersion version
        )
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { LuaValue.NewString("0x1a") }, isMethodCall: false);

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(26d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesUppercaseHexPrefixWithoutBase(
            LuaCompatibilityVersion version
        )
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { LuaValue.NewString("0X1A") }, isMethodCall: false);

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(26d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesNegativeHexStringWithoutBase(
            LuaCompatibilityVersion version
        )
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("-0x10") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(-16d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesPositiveHexStringWithPlusSign(
            LuaCompatibilityVersion version
        )
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("+0x10") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(16d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexStringWithWhitespace(LuaCompatibilityVersion version)
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("  0xFF  ") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(255d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberReturnsNilForInvalidHexString(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            // "0x" without digits is invalid
            CallbackArguments args = new(new[] { LuaValue.NewString("0x") }, isMethodCall: false);

            LuaValue result = BasicModule.ToNumber(context, args);
            LuaValue thousandsResult = BasicModule.ToNumber(
                context,
                new CallbackArguments(new[] { LuaValue.NewString("1,000") }, isMethodCall: false)
            );
            LuaValue trailingPoint = BasicModule.ToNumber(
                context,
                new CallbackArguments(new[] { LuaValue.NewString("1.") }, isMethodCall: false)
            );
            LuaValue exponent = BasicModule.ToNumber(
                context,
                new CallbackArguments(new[] { LuaValue.NewString("1e0") }, isMethodCall: false)
            );
            LuaValue overflow = BasicModule.ToNumber(
                context,
                new CallbackArguments(new[] { LuaValue.NewString("1e999999") }, isMethodCall: false)
            );
            LuaValue underflow = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("1e-999999") },
                    isMethodCall: false
                )
            );
            LuaValue highDecimal = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("9223372036854775807") },
                    isMethodCall: false
                )
            );
            LuaValue highDecimalPlusOne = script.DoString(
                "return tonumber('9223372036854775807') + 1"
            );
            LuaValue negativeZero = BasicModule.ToNumber(
                context,
                new CallbackArguments(new[] { LuaValue.NewString("-0") }, isMethodCall: false)
            );
            LuaValue compensatedHexFloat = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("0x" + new string('f', 400) + "p-1600") },
                    isMethodCall: false
                )
            );
            LuaValue subnormalHexFloat = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("0xffffffffffffffffp-1138") },
                    isMethodCall: false
                )
            );
            LuaValue roundingHexFloat = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("0x220e087835b925585p376") },
                    isMethodCall: false
                )
            );
            LuaValue unicodeExponent = BasicModule.ToNumber(
                context,
                new CallbackArguments(new[] { LuaValue.NewString("0x1p١") }, isMethodCall: false)
            );

            await Assert.That(result.IsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(thousandsResult.IsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(trailingPoint.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(trailingPoint.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(exponent.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(exponent.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(double.IsPositiveInfinity(overflow.Number)).IsTrue();
            await Assert.That(underflow.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(underflow.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(compensatedHexFloat.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert
                .That(subnormalHexFloat.Number)
                .IsEqualTo(double.Epsilon)
                .ConfigureAwait(false);
            long expectedRoundingBits = ((long)(441 + 1023) << 52) | 0x107043C1ADC93L;
            await Assert
                .That(BitConverter.DoubleToInt64Bits(roundingHexFloat.Number))
                .IsEqualTo(expectedRoundingBits)
                .ConfigureAwait(false);
            await Assert.That(unicodeExponent.IsNil).IsTrue().ConfigureAwait(false);
            if (version <= LuaCompatibilityVersion.Lua52)
            {
                await Assert.That(highDecimal.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
                await Assert
                    .That(highDecimalPlusOne.LuaNumber.IsFloat)
                    .IsTrue()
                    .ConfigureAwait(false);
                await Assert.That(highDecimalPlusOne.Number).IsGreaterThan(9e18);
                await Assert.That(negativeZero.LuaNumber.IsFloat).IsTrue();
                await Assert.That(1d / negativeZero.Number).IsEqualTo(double.NegativeInfinity);
            }
            else
            {
                await Assert.That(highDecimal.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
                await Assert
                    .That(highDecimal.LuaNumber.AsInteger)
                    .IsEqualTo(long.MaxValue)
                    .ConfigureAwait(false);
                await Assert
                    .That(highDecimalPlusOne.LuaNumber.AsInteger)
                    .IsEqualTo(long.MinValue)
                    .ConfigureAwait(false);
                await Assert.That(negativeZero.LuaNumber.IsInteger).IsTrue();
                await Assert.That(negativeZero.LuaNumber.AsInteger).IsEqualTo(0L);
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberReturnsNilForHexStringWithInvalidChars(
            LuaCompatibilityVersion version
        )
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            // "0xG" contains invalid hex digit
            CallbackArguments args = new(new[] { LuaValue.NewString("0xG") }, isMethodCall: false);

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.IsNil).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesLargeHexStringWithoutBase(LuaCompatibilityVersion version)
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("0xDeAdBeEf") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(3735928559d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexFloatWithFraction(LuaCompatibilityVersion version)
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            // 0x1.8 = 1 + 8/16 = 1.5, p0 means * 2^0 = 1.5
            CallbackArguments args = new(
                new[] { LuaValue.NewString("0x1.8p0") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(1.5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexFloatWithExponent(LuaCompatibilityVersion version)
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            // 0x1p2 = 1 * 2^2 = 4
            CallbackArguments args = new(
                new[] { LuaValue.NewString("0x1p2") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexFloatWithNegativeExponent(
            LuaCompatibilityVersion version
        )
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            // 0x10p-2 = 16 * 2^(-2) = 16 / 4 = 4
            CallbackArguments args = new(
                new[] { LuaValue.NewString("0x10p-2") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }

        // ========================================
        // Version-Specific Hex Parsing Tests (Lua §3.1)
        // Every reference Lua accepts hexadecimal numerals in tonumber string conversions:
        // Lua 5.1 converts through strtod (hex-capable), and Lua 5.2+ scan hex directly.
        // ========================================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexStringInEveryVersion(LuaCompatibilityVersion version)
        {
            // tonumber('0xFF') parses in all versions: float 255.0 pre-5.3, integer 255 in 5.3+
            Script script = new Script(version, CoreModulePresets.Complete);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { LuaValue.NewString("0xFF") }, isMethodCall: false);

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(255d).ConfigureAwait(false);
            await Assert
                .That(result.IsInteger)
                .IsEqualTo(version >= LuaCompatibilityVersion.Lua53)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesNegativeHexStringInEveryVersion(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("-0x10") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.Number).IsEqualTo(-16d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexFloatsInEveryVersion(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue normal = BasicModule.ToNumber(
                context,
                new CallbackArguments(new[] { LuaValue.NewString("0x1.8p0") }, isMethodCall: false)
            );
            LuaValue overflow = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("0x1p999999999999") },
                    isMethodCall: false
                )
            );
            LuaValue underflow = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("0x1p-999999999999") },
                    isMethodCall: false
                )
            );

            await Assert.That(normal.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(normal.Number).IsEqualTo(1.5d).ConfigureAwait(false);
            await Assert.That(double.IsPositiveInfinity(overflow.Number)).IsTrue();
            await Assert.That(underflow.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(underflow.Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesLargeHexIntegerWithFullPrecision(
            LuaCompatibilityVersion version
        )
        {
            // Test that large hex integers are parsed with full 64-bit precision
            // 0x7FFFFFFFFFFFFFFF = long.MaxValue = 9223372036854775807
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("0x7FFFFFFFFFFFFFFF") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            if (version <= LuaCompatibilityVersion.Lua52)
            {
                // Lua 5.1/5.2 convert hex strings through strtod into doubles
                await Assert.That(result.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
                await Assert.That(result.Number).IsGreaterThan(9e18).ConfigureAwait(false);
            }
            else
            {
                await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
                await Assert
                    .That(result.LuaNumber.AsInteger)
                    .IsEqualTo(long.MaxValue)
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexIntegerWithValueNearMaxLong(
            LuaCompatibilityVersion version
        )
        {
            // 0x123456789ABCDEF = 81985529216486895 (within long range)
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("0x123456789ABCDEF") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            if (version <= LuaCompatibilityVersion.Lua52)
            {
                await Assert.That(result.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
                await Assert.That(result.Number).IsGreaterThan(8e16).ConfigureAwait(false);
            }
            else
            {
                await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
                await Assert
                    .That(result.LuaNumber.AsInteger)
                    .IsEqualTo(81985529216486895L)
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexIntegerExceedingLongAsFloat(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue fullMask = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("0xFFFFFFFFFFFFFFFF") },
                    isMethodCall: false
                )
            );
            LuaValue wrappedZero = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("0x10000000000000000") },
                    isMethodCall: false
                )
            );
            LuaValue negativeFullMask = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("-0xFFFFFFFFFFFFFFFF") },
                    isMethodCall: false
                )
            );
            LuaValue roundingValue = BasicModule.ToNumber(
                context,
                new CallbackArguments(
                    new[] { LuaValue.NewString("0x220e087835b925585") },
                    isMethodCall: false
                )
            );

            if (version <= LuaCompatibilityVersion.Lua52)
            {
                await Assert.That(fullMask.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
                await Assert.That(fullMask.Number).IsGreaterThan(1.8e19).ConfigureAwait(false);
                await Assert.That(wrappedZero.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
                await Assert.That(wrappedZero.Number).IsGreaterThan(1.8e19).ConfigureAwait(false);
                await Assert
                    .That(negativeFullMask.LuaNumber.IsFloat)
                    .IsTrue()
                    .ConfigureAwait(false);
                await Assert
                    .That(negativeFullMask.Number)
                    .IsLessThan(-1.8e19)
                    .ConfigureAwait(false);
                long expectedRoundingBits = ((long)(65 + 1023) << 52) | 0x107043C1ADC93L;
                await Assert
                    .That(BitConverter.DoubleToInt64Bits(roundingValue.Number))
                    .IsEqualTo(expectedRoundingBits)
                    .ConfigureAwait(false);
            }
            else
            {
                await Assert.That(fullMask.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
                await Assert
                    .That(fullMask.LuaNumber.AsInteger)
                    .IsEqualTo(-1L)
                    .ConfigureAwait(false);
                await Assert.That(wrappedZero.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
                await Assert
                    .That(wrappedZero.LuaNumber.AsInteger)
                    .IsEqualTo(0L)
                    .ConfigureAwait(false);
                await Assert
                    .That(negativeFullMask.LuaNumber.IsInteger)
                    .IsTrue()
                    .ConfigureAwait(false);
                await Assert
                    .That(negativeFullMask.LuaNumber.AsInteger)
                    .IsEqualTo(1L)
                    .ConfigureAwait(false);
                await Assert.That(roundingValue.LuaNumber.IsInteger).IsTrue();
                await Assert
                    .That(roundingValue.LuaNumber.AsInteger)
                    .IsEqualTo(unchecked((long)0x20E087835B925585UL));
            }
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesNegativeMaxLongCorrectly(LuaCompatibilityVersion version)
        {
            // -0x8000000000000000 = long.MinValue = -9223372036854775808
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString("-0x8000000000000000") },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            if (version <= LuaCompatibilityVersion.Lua52)
            {
                await Assert.That(result.LuaNumber.IsFloat).IsTrue().ConfigureAwait(false);
                await Assert.That(result.Number).IsLessThan(-9e18).ConfigureAwait(false);
            }
            else
            {
                await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
                await Assert
                    .That(result.LuaNumber.AsInteger)
                    .IsEqualTo(long.MinValue)
                    .ConfigureAwait(false);
            }
        }

        // ========================================
        // Infinity/NaN String Parsing Tests (Lua §6.1)
        // Lua 5.1 accepts "inf" and "nan" string literals via C's strtod.
        // Lua 5.2+ rejects them and returns nil.
        // ========================================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("inf")]
        [global::TUnit.Core.Arguments("Inf")]
        [global::TUnit.Core.Arguments("INF")]
        [global::TUnit.Core.Arguments("infinity")]
        [global::TUnit.Core.Arguments("Infinity")]
        [global::TUnit.Core.Arguments("INFINITY")]
        public async Task ToNumberParsesInfStringInLua51(string infString)
        {
            // In Lua 5.1, tonumber('inf') returns positive infinity
            Script script = CreateScript(LuaCompatibilityVersion.Lua51);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString(infString) },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("-inf")]
        [global::TUnit.Core.Arguments("-Inf")]
        [global::TUnit.Core.Arguments("-INF")]
        [global::TUnit.Core.Arguments("-infinity")]
        [global::TUnit.Core.Arguments("-Infinity")]
        [global::TUnit.Core.Arguments("-INFINITY")]
        public async Task ToNumberParsesNegativeInfStringInLua51(string infString)
        {
            // In Lua 5.1, tonumber('-inf') returns negative infinity
            Script script = CreateScript(LuaCompatibilityVersion.Lua51);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString(infString) },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert
                .That(double.IsNegativeInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberReturnsNilForInfStringInLua52Plus(LuaCompatibilityVersion version)
        {
            // In Lua 5.2+, tonumber('inf') returns nil
            Script script = new Script(version, CoreModulePresets.Complete);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { LuaValue.NewString("inf") }, isMethodCall: false);

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.IsNil).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("nan")]
        [global::TUnit.Core.Arguments("NaN")]
        [global::TUnit.Core.Arguments("NAN")]
        [global::TUnit.Core.Arguments("Nan")]
        [global::TUnit.Core.Arguments("+nan")]
        [global::TUnit.Core.Arguments("+NaN")]
        public async Task ToNumberParsesNanStringAsPositiveNanInLua51(string nanString)
        {
            // In Lua 5.1, tonumber('nan') returns a positive NaN (per strtod behavior on Linux)
            Script script = CreateScript(LuaCompatibilityVersion.Lua51);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString(nanString) },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
            // Verify it's a positive NaN (sign bit not set)
            await Assert.That(double.IsNegative(result.Number)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("-nan")]
        [global::TUnit.Core.Arguments("-NaN")]
        [global::TUnit.Core.Arguments("-NAN")]
        [global::TUnit.Core.Arguments("-Nan")]
        public async Task ToNumberParsesNegativeNanStringAsNegativeNanInLua51(string nanString)
        {
            // In Lua 5.1, tonumber('-nan') returns a negative NaN (sign bit set)
            Script script = CreateScript(LuaCompatibilityVersion.Lua51);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { LuaValue.NewString(nanString) },
                isMethodCall: false
            );

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
            // Verify it's a negative NaN (sign bit set)
            await Assert.That(double.IsNegative(result.Number)).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberReturnsNilForNanStringInLua52Plus(LuaCompatibilityVersion version)
        {
            // In Lua 5.2+, tonumber('nan') returns nil
            Script script = new Script(version, CoreModulePresets.Complete);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(new[] { LuaValue.NewString("nan") }, isMethodCall: false);

            LuaValue result = BasicModule.ToNumber(context, args);

            await Assert.That(result.IsNil).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelectErrorsOnNonIntegerIndexLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

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
            Script script = new Script(version, CoreModulePresets.Complete);

            // select(1.5, 'a', 'b', 'c') should truncate to 1 and return all elements
            LuaValue result = script.DoString("return select(1.5, 'a', 'b', 'c')");

            // 1.5 floors to 1, so returns all 3 arguments
            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelectAcceptsIntegralFloatLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // select(2.0, 'a', 'b', 'c') should work since 2.0 has integer representation
            LuaValue result = script.DoString("return select(2.0, 'a', 'b', 'c')");

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
            Script script = new Script(version, CoreModulePresets.Complete);

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
            Script script = new Script(version, CoreModulePresets.Complete);

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
            Script script = new Script(version, CoreModulePresets.Complete);
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
            Script script = new Script(version, CoreModulePresets.Complete);
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
            Script script = new Script(version, CoreModulePresets.Complete);
            List<string> output = new();
            script.Options.DebugPrint = output.Add;

            // Override global tostring and cover both a present metatable without __tostring
            // and an explicitly nil __tostring entry.
            script.DoString(
                @"
                function tostring(v)
                    return 'CUSTOM:' .. type(v)
                end
                no_field = setmetatable({}, {})
                nil_field = setmetatable({}, { __tostring = nil })
                print(no_field)
                print(nil_field)
            "
            );

            // In Lua 5.4+, print uses default formatting for tables without __tostring
            // Should print something like "table: 0x..." not "CUSTOM:table"
            await Assert.That(output.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(output[0]).Contains("table:").ConfigureAwait(false);
            await Assert.That(output[1]).Contains("table:").ConfigureAwait(false);
            await Assert.That(output[0]).DoesNotContain("CUSTOM").ConfigureAwait(false);
            await Assert.That(output[1]).DoesNotContain("CUSTOM").ConfigureAwait(false);
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
            Script script = new Script(version, CoreModulePresets.Complete);
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
            Script script = new Script(version, CoreModulePresets.Complete);
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
            Script script = new Script(version, CoreModulePresets.Complete);
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
            Script script = new Script(version, CoreModulePresets.Complete);
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
            Script script = new Script(version, CoreModulePresets.Complete);
            string output = null;
            script.Options.DebugPrint = s => output = s;

            // Replace global tostring with a CLR callback
            script.Globals["tostring"] = LuaValue.NewCallback(
                (_, args) =>
                {
                    return LuaValue.NewString("CLR:" + args[0].Type);
                }
            );

            script.DoString("print({})");

            // CLR tostring should be called
            await Assert.That(output).IsEqualTo("CLR:Table").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisteredBasicCallbacksUseArgumentViews(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            List<string> callbackNames = new()
            {
                "type",
                "assert",
                "collectgarbage",
                "error",
                "tostring",
                "select",
                "tonumber",
                "print",
            };

            if (version == LuaCompatibilityVersion.Lua51)
            {
                callbackNames.Add("getfenv");
                callbackNames.Add("setfenv");
            }

            if (version >= LuaCompatibilityVersion.Lua54)
            {
                callbackNames.Add("warn");
            }

            for (int i = 0; i < callbackNames.Count; i++)
            {
                string callbackName = callbackNames[i];
                CallbackFunction callback = script.Globals.Get(callbackName).Callback;
                await Assert
                    .That(callback.HasArgumentViewCallback)
                    .IsTrue()
                    .Because($"basic.{callbackName} should use stack-only arguments")
                    .ConfigureAwait(false);
            }

            List<string> printed = new();
            script.Options.DebugPrint = s => printed.Add(s);

            LuaValue result = script.DoString(
                @"
local count = select('#', 'a', 'b', 'c')
assert(count == 3, 'select count mismatch')
assert(select(2, 'a', 'b', 'c') == 'b')
print('value:', 42)
return tostring(42)
"
            );

            await Assert.That(result.String).IsEqualTo("42").ConfigureAwait(false);
            await Assert.That(printed.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(printed[0]).IsEqualTo("value:\t42").ConfigureAwait(false);

            if (version == LuaCompatibilityVersion.Lua51)
            {
                LuaValue fenvResult = script.DoString(
                    @"
local function reader() return payload end
setfenv(reader, { payload = 7 })
return reader()
"
                );
                await Assert.That(fenvResult.Number).IsEqualTo(7d).ConfigureAwait(false);
            }

            if (version >= LuaCompatibilityVersion.Lua54)
            {
                List<string> warnings = new();
                script.Globals.Set(
                    "_WARN",
                    LuaValue.NewCallback(
                        (_, warnArgs) =>
                        {
                            warnings.Add(warnArgs[0].String);
                            return LuaValue.Nil;
                        }
                    )
                );
                script.DoString("warn('@on'); warn('caution', 9); warn('@off')");

                await Assert.That(warnings.Count).IsEqualTo(1).ConfigureAwait(false);
                await Assert.That(warnings[0]).IsEqualTo("caution9").ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task BasicModuleViewMethodsAvoidLegacyArrayMaterialization()
        {
            MethodInfo getArray = RequireMethod(
                typeof(CallbackArgumentsView),
                nameof(CallbackArgumentsView.GetArray),
                typeof(int)
            );

            Dictionary<string, string> viewMethodNames = new()
            {
                ["type"] = nameof(BasicModule.Type),
                ["assert"] = nameof(BasicModule.Assert),
                ["collectgarbage"] = nameof(BasicModule.CollectGarbage),
                ["error"] = nameof(BasicModule.Error),
                ["tostring"] = nameof(BasicModule.ToString),
                ["select"] = nameof(BasicModule.Select),
                ["tonumber"] = nameof(BasicModule.ToNumber),
                ["print"] = nameof(BasicModule.Print),
                ["getfenv"] = nameof(BasicModule.GetFenv),
                ["setfenv"] = nameof(BasicModule.SetFenv),
                ["warn"] = nameof(BasicModule.Warn),
            };

            foreach (KeyValuePair<string, string> pair in viewMethodNames)
            {
                MethodInfo viewMethod = RequireMethod(
                    typeof(BasicModule),
                    pair.Value,
                    typeof(ScriptExecutionContext),
                    typeof(CallbackArgumentsView)
                );

                await Assert
                    .That(viewMethod.IsPrivate)
                    .IsTrue()
                    .Because($"{pair.Key} must register the argument-view implementation")
                    .ConfigureAwait(false);

                int expectedGetArrayCalls = pair.Key == "assert" ? 1 : 0;
                await Assert
                    .That(CountMethodCalls(viewMethod, getArray))
                    .IsEqualTo(expectedGetArrayCalls)
                    .Because(
                        $"basic.{pair.Key} must not materialize legacy argument arrays "
                            + "beyond its escaped-tuple contract"
                    )
                    .ConfigureAwait(false);
            }
        }

        private static CallbackFunction CreateToStringContinuationOnCurrentThread()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue value = script.DoString(
                "return setmetatable({}, { __tostring = function() return 'value' end })"
            );
            CallbackArguments args = new(new[] { value }, isMethodCall: false);
            LuaValue request = BasicModule.ToString(context, args);

            if (request.Type != DataType.TailCallRequest)
            {
                throw new InvalidOperationException("tostring did not return a tail call request.");
            }

            return request.TailCallData.Continuation;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Worker thread exceptions must be marshaled back to the test thread."
        )]
        private static T RunOnNewThread<T>(Func<T> action)
        {
            T result = default;
            Exception error = null;
            Thread thread = new(() =>
            {
                try
                {
                    result = action();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            })
            {
                IsBackground = true,
            };

            thread.Start();
            if (!thread.Join(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Worker thread did not finish.");
            }

            if (error != null)
            {
                throw new InvalidOperationException("Worker thread failed.", error);
            }

            return result;
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

        private static MethodInfo RequireMethod(
            Type type,
            string name,
            params Type[] parameterTypes
        )
        {
            const BindingFlags flags =
                BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.Static;
            return RequireMethod(type, name, flags, parameterTypes);
        }

        private static MethodInfo RequireMethod(
            Type type,
            string name,
            BindingFlags bindingFlags,
            params Type[] parameterTypes
        )
        {
            MethodInfo method = type.GetMethod(name, bindingFlags, null, parameterTypes, null);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, name);
            }

            return method;
        }

        private static int CountMethodCalls(MethodInfo method, MethodInfo target)
        {
            MethodBody body = method.GetMethodBody();
            byte[] il = body?.GetILAsByteArray() ?? Array.Empty<byte>();
            Module module = method.Module;
            Type[] typeArguments = method.DeclaringType?.GetGenericArguments() ?? Type.EmptyTypes;
            Type[] methodArguments = method.GetGenericArguments();
            int count = 0;

            for (int offset = 0; offset < il.Length; )
            {
                OpCode opCode = ReadOpCode(il, ref offset);
                if (opCode.OperandType == OperandType.InlineMethod)
                {
                    int token = BitConverter.ToInt32(il, offset);
                    offset += sizeof(int);
                    MethodBase resolved = module.ResolveMethod(
                        token,
                        typeArguments,
                        methodArguments
                    );
                    if (
                        resolved.Module == target.Module
                        && resolved.MetadataToken == target.MetadataToken
                    )
                    {
                        count++;
                    }
                    continue;
                }

                offset += GetOperandSize(opCode.OperandType, il, offset);
            }

            return count;
        }

        private static OpCode ReadOpCode(byte[] il, ref int offset)
        {
            byte value = il[offset++];
            if (value == 0xfe)
            {
                return MultiByteOpCodes[il[offset++]];
            }

            return SingleByteOpCodes[value];
        }

        private static int GetOperandSize(OperandType operandType, byte[] il, int offset)
        {
            switch (operandType)
            {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;
                case OperandType.InlineVar:
                    return 2;
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    return 4;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                case OperandType.InlineSwitch:
                    int targetCount = BitConverter.ToInt32(il, offset);
                    return sizeof(int) + (targetCount * sizeof(int));
                default:
                    throw new NotSupportedException($"Unsupported IL operand type: {operandType}");
            }
        }
    }
}
