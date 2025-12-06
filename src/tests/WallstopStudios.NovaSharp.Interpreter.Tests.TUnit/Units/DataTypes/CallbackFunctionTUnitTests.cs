namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Options;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    public sealed class CallbackFunctionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenCallbackIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new CallbackFunction(null);
            });

            await Assert.That(exception.ParamName).IsEqualTo("callBack").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InvokeThrowsWhenExecutionContextIsNull()
        {
            CallbackFunction function = new((_, _) => DynValue.Nil);
            List<DynValue> arguments = new() { DynValue.NewNumber(1) };

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                function.Invoke(null, arguments)
            );

            await Assert
                .That(exception.ParamName)
                .IsEqualTo("executionContext")
                .ConfigureAwait(false);
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

            await Assert.That(exception.ParamName).IsEqualTo("args").ConfigureAwait(false);
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

            await Assert.That(captured).IsNotNull().ConfigureAwait(false);
            await Assert.That(captured!.IsMethodCall).IsFalse().ConfigureAwait(false);
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
            await Assert.That(captured).IsNotNull().ConfigureAwait(false);
            await Assert.That(captured!.IsMethodCall).IsFalse().ConfigureAwait(false);

            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<SampleUserData>(ensureUnregistered: true);
            registrationScope.RegisterType<SampleUserData>();

            DynValue userData = UserData.Create(new SampleUserData());
            List<DynValue> userDataArgs = new() { userData };

            function.Invoke(context, userDataArgs, isMethodCall: true);
            await Assert.That(captured).IsNotNull().ConfigureAwait(false);
            await Assert.That(captured!.IsMethodCall).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultAccessModeRejectsUnsupportedValues()
        {
            using StaticValueScope<InteropAccessMode> modeScope =
                StaticValueScope<InteropAccessMode>.Capture(
                    () => CallbackFunction.DefaultAccessMode,
                    value => CallbackFunction.DefaultAccessMode = value
                );

            ArgumentException defaultException = Assert.Throws<ArgumentException>(() =>
                CallbackFunction.DefaultAccessMode = InteropAccessMode.Default
            );
            ArgumentException hideMembers = Assert.Throws<ArgumentException>(() =>
                CallbackFunction.DefaultAccessMode = InteropAccessMode.HideMembers
            );
            ArgumentException backgroundOptimized = Assert.Throws<ArgumentException>(() =>
                CallbackFunction.DefaultAccessMode = InteropAccessMode.BackgroundOptimized
            );

            await Assert.That(defaultException).IsNotNull().ConfigureAwait(false);
            await Assert.That(hideMembers).IsNotNull().ConfigureAwait(false);
            await Assert.That(backgroundOptimized).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FromDelegateUsesConfiguredDefaultAccessMode()
        {
            Script script = new();
            using StaticValueScope<InteropAccessMode> modeScope =
                StaticValueScope<InteropAccessMode>.Override(
                    () => CallbackFunction.DefaultAccessMode,
                    value => CallbackFunction.DefaultAccessMode = value,
                    InteropAccessMode.Reflection
                );

            CallbackFunction function = CallbackFunction.FromDelegate(
                script,
                new Func<int, int>(SampleUserData.AddOne)
            );

            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            List<DynValue> args = new() { DynValue.NewNumber(41) };

            DynValue result = function.Invoke(context, args);
            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FromDelegateThrowsWhenScriptIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                CallbackFunction.FromDelegate(null, new Func<int, int>(SampleUserData.AddOne))
            );

            await Assert.That(exception.ParamName).IsEqualTo("script").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FromDelegateThrowsWhenDelegateIsNull()
        {
            Script script = new();
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                CallbackFunction.FromDelegate(script, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("del").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FromMethodInfoThrowsWhenScriptIsNull()
        {
            MethodInfo method = SampleUserData.GetPublicCallbackMethod();
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                CallbackFunction.FromMethodInfo(null, method)
            );

            await Assert.That(exception.ParamName).IsEqualTo("script").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FromMethodInfoThrowsWhenMethodInfoIsNull()
        {
            Script script = new();
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                CallbackFunction.FromMethodInfo(script, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("mi").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckCallbackSignatureHonoursVisibilityRequirement()
        {
            MethodInfo publicMethod = SampleUserData.GetPublicCallbackMethod();
            MethodInfo internalMethod = SampleUserData.GetInternalCallbackMethod();
            MethodInfo badMethod = SampleUserData.GetBadSignatureMethod();

            await Assert
                .That(CallbackFunction.CheckCallbackSignature(publicMethod, true))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(CallbackFunction.CheckCallbackSignature(publicMethod, false))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(CallbackFunction.CheckCallbackSignature(internalMethod, true))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(CallbackFunction.CheckCallbackSignature(internalMethod, false))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(CallbackFunction.CheckCallbackSignature(badMethod, true))
                .IsFalse()
                .ConfigureAwait(false);
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
