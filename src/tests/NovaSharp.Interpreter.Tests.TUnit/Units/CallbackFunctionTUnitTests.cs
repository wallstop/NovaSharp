#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Options;
    using NovaSharp.Interpreter.Tests.Units;

    public sealed class CallbackFunctionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenCallbackIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new CallbackFunction(null);
            });

            await Assert.That(exception.ParamName).IsEqualTo("callBack");
        }

        [global::TUnit.Core.Test]
        public async Task InvokeThrowsWhenExecutionContextIsNull()
        {
            CallbackFunction function = new((_, _) => DynValue.Nil);
            List<DynValue> arguments = new() { DynValue.NewNumber(1) };

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                function.Invoke(null, arguments)
            );

            await Assert.That(exception.ParamName).IsEqualTo("executionContext");
        }

        [global::TUnit.Core.Test]
        public async Task InvokeThrowsWhenArgumentsAreNull()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackFunction function = new((_, _) => DynValue.Nil);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                function.Invoke(context, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task InvokeTreatsColonAsRegularCallWhenConfigured()
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

            await Assert.That(captured).IsNotNull();
            await Assert.That(captured!.IsMethodCall).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task InvokeTreatsMethodCallsOnlyForUserDataUnderDotBehaviour()
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
            await Assert.That(captured).IsNotNull();
            await Assert.That(captured!.IsMethodCall).IsFalse();

            if (!UserData.IsTypeRegistered<SampleUserData>())
            {
                UserData.RegisterType<SampleUserData>();
            }

            DynValue userData = UserData.Create(new SampleUserData());
            List<DynValue> userDataArgs = new() { userData };

            function.Invoke(context, userDataArgs, isMethodCall: true);
            await Assert.That(captured).IsNotNull();
            await Assert.That(captured!.IsMethodCall).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DefaultAccessModeRejectsUnsupportedValues()
        {
            InteropAccessMode original = CallbackFunction.DefaultAccessMode;

            try
            {
                ArgumentException defaultException = Assert.Throws<ArgumentException>(() =>
                    CallbackFunction.DefaultAccessMode = InteropAccessMode.Default
                );
                ArgumentException hideMembers = Assert.Throws<ArgumentException>(() =>
                    CallbackFunction.DefaultAccessMode = InteropAccessMode.HideMembers
                );
                ArgumentException backgroundOptimized = Assert.Throws<ArgumentException>(() =>
                    CallbackFunction.DefaultAccessMode = InteropAccessMode.BackgroundOptimized
                );

                await Assert.That(defaultException).IsNotNull();
                await Assert.That(hideMembers).IsNotNull();
                await Assert.That(backgroundOptimized).IsNotNull();
            }
            finally
            {
                CallbackFunction.DefaultAccessMode = original;
            }
        }

        [global::TUnit.Core.Test]
        public async Task FromDelegateUsesConfiguredDefaultAccessMode()
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
                await Assert.That(result.Number).IsEqualTo(42d);
            }
            finally
            {
                CallbackFunction.DefaultAccessMode = original;
            }
        }

        [global::TUnit.Core.Test]
        public async Task FromDelegateThrowsWhenScriptIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                CallbackFunction.FromDelegate(null, new Func<int, int>(SampleUserData.AddOne))
            );

            await Assert.That(exception.ParamName).IsEqualTo("script");
        }

        [global::TUnit.Core.Test]
        public async Task FromDelegateThrowsWhenDelegateIsNull()
        {
            Script script = new();
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                CallbackFunction.FromDelegate(script, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("del");
        }

        [global::TUnit.Core.Test]
        public async Task FromMethodInfoThrowsWhenScriptIsNull()
        {
            MethodInfo method = SampleUserData.GetPublicCallbackMethod();
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                CallbackFunction.FromMethodInfo(null, method)
            );

            await Assert.That(exception.ParamName).IsEqualTo("script");
        }

        [global::TUnit.Core.Test]
        public async Task FromMethodInfoThrowsWhenMethodInfoIsNull()
        {
            Script script = new();
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                CallbackFunction.FromMethodInfo(script, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("mi");
        }

        [global::TUnit.Core.Test]
        public async Task CheckCallbackSignatureHonoursVisibilityRequirement()
        {
            MethodInfo publicMethod = SampleUserData.GetPublicCallbackMethod();
            MethodInfo internalMethod = SampleUserData.GetInternalCallbackMethod();
            MethodInfo badMethod = SampleUserData.GetBadSignatureMethod();

            await Assert.That(CallbackFunction.CheckCallbackSignature(publicMethod, true)).IsTrue();
            await Assert
                .That(CallbackFunction.CheckCallbackSignature(publicMethod, false))
                .IsTrue();
            await Assert
                .That(CallbackFunction.CheckCallbackSignature(internalMethod, true))
                .IsTrue();
            await Assert
                .That(CallbackFunction.CheckCallbackSignature(internalMethod, false))
                .IsFalse();
            await Assert.That(CallbackFunction.CheckCallbackSignature(badMethod, true)).IsFalse();
        }

        private sealed class SampleUserData
        {
            private static readonly MethodInfo ValidCallbackMethodInfo = (
                (Func<ScriptExecutionContext, CallbackArguments, DynValue>)ValidCallback
            ).Method;

            private static readonly MethodInfo PrivateCallbackMethodInfo = (
                (Func<ScriptExecutionContext, CallbackArguments, DynValue>)PrivateCallback
            ).Method;

            private static readonly MethodInfo BadSignatureMethodInfo = (
                (Func<ScriptExecutionContext, int, DynValue>)BadSignature
            ).Method;

            public static int AddOne(int value)
            {
                return value + 1;
            }

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

            public static MethodInfo GetPublicCallbackMethod()
            {
                return ValidCallbackMethodInfo;
            }

            public static MethodInfo GetInternalCallbackMethod()
            {
                return PrivateCallbackMethodInfo;
            }

            public static MethodInfo GetBadSignatureMethod()
            {
                return BadSignatureMethodInfo;
            }
        }
    }
}
#pragma warning restore CA2007
