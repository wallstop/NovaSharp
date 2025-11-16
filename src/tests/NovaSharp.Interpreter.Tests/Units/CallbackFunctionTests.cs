namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Options;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CallbackFunctionTests
    {
        [Test]
        public void InvokeTreatsColonAsRegularCallWhenConfigured()
        {
            Script script = new();
            script.Options.ColonOperatorClrCallbackBehaviour = ColonOperatorBehaviour.TreatAsColon;
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            CallbackArguments captured = null;
            CallbackFunction function = new(
                (_, args) =>
                {
                    captured = args;
                    return DynValue.Nil;
                }
            );

            List<DynValue> arguments = new() { DynValue.NewNumber(1), DynValue.NewNumber(2) };

            function.Invoke(context, arguments, isMethodCall: true);

            Assert.Multiple(() =>
            {
                Assert.That(captured, Is.Not.Null);
                Assert.That(captured!.IsMethodCall, Is.False);
            });
        }

        [Test]
        public void InvokeTreatsMethodCallsOnlyForUserDataUnderDotBehaviour()
        {
            Script script = new();
            script.Options.ColonOperatorClrCallbackBehaviour =
                ColonOperatorBehaviour.TreatAsDotOnUserData;
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            CallbackArguments captured = null;
            CallbackFunction function = new(
                (_, args) =>
                {
                    captured = args;
                    return DynValue.Nil;
                }
            );

            List<DynValue> nonUserData = new() { DynValue.NewString("self") };
            function.Invoke(context, nonUserData, isMethodCall: true);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured.IsMethodCall, Is.False);

            if (!UserData.IsTypeRegistered<SampleUserData>())
            {
                UserData.RegisterType<SampleUserData>();
            }
            DynValue userData = UserData.Create(new SampleUserData());
            List<DynValue> userDataArgs = new() { userData };

            function.Invoke(context, userDataArgs, isMethodCall: true);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured.IsMethodCall, Is.True);
        }

        [Test]
        public void DefaultAccessModeRejectsUnsupportedValues()
        {
            InteropAccessMode original = CallbackFunction.DefaultAccessMode;

            try
            {
                Assert.Multiple(() =>
                {
                    Assert.That(
                        () => CallbackFunction.DefaultAccessMode = InteropAccessMode.Default,
                        Throws.TypeOf<ArgumentException>()
                    );
                    Assert.That(
                        () => CallbackFunction.DefaultAccessMode = InteropAccessMode.HideMembers,
                        Throws.TypeOf<ArgumentException>()
                    );
                    Assert.That(
                        () =>
                            CallbackFunction.DefaultAccessMode =
                                InteropAccessMode.BackgroundOptimized,
                        Throws.TypeOf<ArgumentException>()
                    );
                });
            }
            finally
            {
                CallbackFunction.DefaultAccessMode = original;
            }
        }

        [Test]
        public void FromDelegateUsesConfiguredDefaultAccessMode()
        {
            Script script = new();
            InteropAccessMode original = CallbackFunction.DefaultAccessMode;

            try
            {
                CallbackFunction.DefaultAccessMode = InteropAccessMode.Reflection;

                CallbackFunction function = CallbackFunction.FromDelegate(
                    script,
                    new Func<int, int>(SampleUserData.AddOne)
                );

                ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
                List<DynValue> args = new() { DynValue.NewNumber(41) };

                DynValue result = function.Invoke(context, args);

                Assert.That(result.Number, Is.EqualTo(42d));
            }
            finally
            {
                CallbackFunction.DefaultAccessMode = original;
            }
        }

        [Test]
        public void CheckCallbackSignatureHonoursVisibilityRequirement()
        {
            System.Reflection.MethodInfo publicMethod = typeof(SampleUserData).GetMethod(
                nameof(SampleUserData.ValidCallback)
            )!;

            System.Reflection.MethodInfo internalMethod = typeof(SampleUserData).GetMethod(
                nameof(SampleUserData.PrivateCallback),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            )!;

            System.Reflection.MethodInfo badMethod = typeof(SampleUserData).GetMethod(
                nameof(SampleUserData.BadSignature)
            )!;

            Assert.Multiple(() =>
            {
                Assert.That(CallbackFunction.CheckCallbackSignature(publicMethod, true), Is.True);
                Assert.That(CallbackFunction.CheckCallbackSignature(publicMethod, false), Is.True);
                Assert.That(CallbackFunction.CheckCallbackSignature(internalMethod, true), Is.True);
                Assert.That(
                    CallbackFunction.CheckCallbackSignature(internalMethod, false),
                    Is.False
                );
                Assert.That(CallbackFunction.CheckCallbackSignature(badMethod, true), Is.False);
            });
        }

        private sealed class SampleUserData
        {
            public static int AddOne(int value) => value + 1;

            public static DynValue ValidCallback(
                ScriptExecutionContext context,
                CallbackArguments args
            )
            {
                return DynValue.NewNumber(args[0].Number + 1);
            }

            internal static DynValue PrivateCallback(
                ScriptExecutionContext context,
                CallbackArguments args
            )
            {
                return DynValue.NewNumber(args[0].Number + 1);
            }

            public static DynValue BadSignature(ScriptExecutionContext context, int value)
            {
                return DynValue.NewNumber(value);
            }
        }
    }
}
