#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;

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

            using ConsoleCaptureScope captureScope = new(captureError: true);

            CallbackArguments args = new(new[] { DynValue.NewString("console-warning") }, false);
            BasicModule.Warn(context, args);

            await Assert.That(captureScope.Writer.ToString()).Contains("console-warning");
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
    }
}
#pragma warning restore CA2007
