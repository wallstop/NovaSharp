namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BasicModuleTests
    {
        [Test]
        public void TypeThrowsWhenArgumentsAreNull()
        {
            Assert.That(() => BasicModule.Type(null, null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TypeThrowsWhenNoArgumentsProvided()
        {
            CallbackArguments args = new(Array.Empty<DynValue>(), isMethodCall: false);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                BasicModule.Type(null, args)
            )!;

            Assert.That(ex.Message, Does.Contain("type"));
        }

        [Test]
        public void CollectGarbageThrowsWhenArgumentsAreNull()
        {
            Assert.That(
                () => BasicModule.CollectGarbage(null, null),
                Throws.TypeOf<ArgumentNullException>()
            );
        }

        [Test]
        public void CollectGarbageRunsWhenModeIsCollect()
        {
            CallbackArguments args = new(new[] { DynValue.Nil }, isMethodCall: false);

            DynValue result = BasicModule.CollectGarbage(null, args);

            Assert.That(result, Is.EqualTo(DynValue.Nil));
        }

        [Test]
        public void CollectGarbageSkipsWhenModeIsNotSupported()
        {
            CallbackArguments args = new(new[] { DynValue.NewString("stop") }, isMethodCall: false);

            DynValue result = BasicModule.CollectGarbage(null, args);

            Assert.That(result, Is.EqualTo(DynValue.Nil));
        }

        [Test]
        public void ToStringContinuationThrowsWhenMetamethodReturnsNonString()
        {
            CallbackArguments args = new(new[] { DynValue.NewNumber(5) }, isMethodCall: false);

            Assert.That(
                () => BasicModule.ToStringContinuation(null, args),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void SelectCountsTupleArgumentsWhenHashRequested()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2));
            CallbackArguments args = new(
                new[] { DynValue.NewString("#"), DynValue.NewNumber(10), tuple },
                false
            );

            DynValue result = BasicModule.Select(null, args);

            Assert.That(result.Number, Is.EqualTo(3));
        }

        [Test]
        public void WarnThrowsWhenExecutionContextIsNull()
        {
            CallbackArguments args = new(new[] { DynValue.NewString("payload") }, false);

            Assert.That(() => BasicModule.Warn(null, args), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void WarnInvokesCustomWarnHandler()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            string observed = null;
            script.Globals.Set(
                "_WARN",
                DynValue.NewCallback(
                    (callbackContext, warnArgs) =>
                    {
                        observed = warnArgs[0].String;
                        return DynValue.Nil;
                    }
                )
            );

            CallbackArguments args = new(new[] { DynValue.NewString("custom-warning") }, false);
            BasicModule.Warn(context, args);

            Assert.That(observed, Is.EqualTo("custom-warning"));
        }

        [Test]
        public void WarnUsesDebugPrintWhenHandlerMissing()
        {
            Script script = new();
            script.Globals.Set("_WARN", DynValue.Nil);
            string observed = null;
            script.Options.DebugPrint = s => observed = s;
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            CallbackArguments args = new(new[] { DynValue.NewString("debug-warning") }, false);
            BasicModule.Warn(context, args);

            Assert.That(observed, Is.EqualTo("debug-warning"));
        }

        [Test]
        public void WarnWritesToConsoleWhenNoHandlerOrDebugPrint()
        {
            Script script = new();
            script.Globals.Set("_WARN", DynValue.Nil);
            script.Options.DebugPrint = null;
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            TextWriter original = Console.Error;
            using StringWriter capture = new();
            Console.SetError(capture);

            try
            {
                CallbackArguments args = new(
                    new[] { DynValue.NewString("console-warning") },
                    false
                );
                BasicModule.Warn(context, args);
            }
            finally
            {
                Console.SetError(original);
            }

            Assert.That(capture.ToString(), Does.Contain("console-warning"));
        }

        [Test]
        public void ToNumberReturnsNilWhenInvalidDigitProvidedForBase()
        {
            Script script = new();
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(
                new[] { DynValue.NewString("17"), DynValue.NewNumber(6) },
                isMethodCall: false
            );

            DynValue result = BasicModule.ToNumber(context, args);

            Assert.That(result.IsNil(), Is.True);
        }
    }
}
