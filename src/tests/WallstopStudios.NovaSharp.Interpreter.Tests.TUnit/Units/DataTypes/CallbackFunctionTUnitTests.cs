namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Options;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

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
            CallbackFunction function = new((_, _) => LuaValue.Nil);
            List<LuaValue> arguments = new() { LuaValue.NewNumber(1) };

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                function.Invoke((ScriptExecutionContext)null, arguments)
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
            CallbackFunction function = new((_, _) => LuaValue.Nil);

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
                    return LuaValue.Nil;
                }
            );

            List<LuaValue> arguments = new() { LuaValue.NewNumber(1), LuaValue.NewNumber(2) };

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
                    return LuaValue.Nil;
                }
            );

            List<LuaValue> nilSelf = new() { default };
            function.Invoke(context, nilSelf, isMethodCall: true);
            await Assert.That(captured).IsNotNull().ConfigureAwait(false);
            await Assert.That(captured!.IsMethodCall).IsFalse().ConfigureAwait(false);

            List<LuaValue> nonUserData = new() { LuaValue.NewString("self") };
            function.Invoke(context, nonUserData, isMethodCall: true);
            await Assert.That(captured).IsNotNull().ConfigureAwait(false);
            await Assert.That(captured!.IsMethodCall).IsFalse().ConfigureAwait(false);

            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<SampleUserData>(ensureUnregistered: true);
            registrationScope.RegisterType<SampleUserData>();

            bool created = UserData.TryCreate(new SampleUserData(), out LuaValue userData);
            await Assert.That(created).IsTrue().ConfigureAwait(false);
            List<LuaValue> userDataArgs = new() { userData };

            function.Invoke(context, userDataArgs, isMethodCall: true);
            await Assert.That(captured).IsNotNull().ConfigureAwait(false);
            await Assert.That(captured!.IsMethodCall).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InvokeArgumentViewTreatsMethodCallsOnlyForUserDataUnderDotBehaviour()
        {
            Script script = new();
            script.Options.ColonOperatorClrCallbackBehaviour =
                ColonOperatorBehaviour.TreatAsDotOnUserData;
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            bool? capturedIsMethodCall = null;
            CallbackFunction function = CallbackFunction.FromArgumentView(
                (_, args) =>
                {
                    capturedIsMethodCall = args.IsMethodCall;
                    return LuaValue.Nil;
                }
            );

            List<LuaValue> nilSelf = new() { default };
            function.Invoke(context, nilSelf, isMethodCall: true);
            await Assert.That(capturedIsMethodCall).IsFalse().ConfigureAwait(false);

            List<LuaValue> nonUserData = new() { LuaValue.NewString("self") };
            function.Invoke(context, nonUserData, isMethodCall: true);
            await Assert.That(capturedIsMethodCall).IsFalse().ConfigureAwait(false);

            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<SampleUserData>(ensureUnregistered: true);
            registrationScope.RegisterType<SampleUserData>();

            bool created = UserData.TryCreate(new SampleUserData(), out LuaValue userData);
            await Assert.That(created).IsTrue().ConfigureAwait(false);
            List<LuaValue> userDataArgs = new() { userData };

            function.Invoke(context, userDataArgs, isMethodCall: true);
            await Assert.That(capturedIsMethodCall).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InvokeNoContextArgumentViewTreatsMethodCallsOnlyForUserDataUnderDotBehaviour()
        {
            Script script = new();
            script.Options.ColonOperatorClrCallbackBehaviour =
                ColonOperatorBehaviour.TreatAsDotOnUserData;

            bool? capturedIsMethodCall = null;
            CallbackFunction function = CallbackFunction.FromArgumentView(
                (CallbackArgumentsView args) =>
                {
                    capturedIsMethodCall = args.IsMethodCall;
                    return LuaValue.Nil;
                }
            );

            List<LuaValue> nilSelf = new() { default };
            function.Invoke(script, nilSelf, isMethodCall: true);
            await Assert.That(capturedIsMethodCall).IsFalse().ConfigureAwait(false);

            List<LuaValue> nonUserData = new() { LuaValue.NewString("self") };
            function.Invoke(script, nonUserData, isMethodCall: true);
            await Assert.That(capturedIsMethodCall).IsFalse().ConfigureAwait(false);

            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<SampleUserData>(ensureUnregistered: true);
            registrationScope.RegisterType<SampleUserData>();

            bool created = UserData.TryCreate(new SampleUserData(), out LuaValue userData);
            await Assert.That(created).IsTrue().ConfigureAwait(false);
            List<LuaValue> userDataArgs = new() { userData };

            function.Invoke(script, userDataArgs, isMethodCall: true);
            await Assert.That(capturedIsMethodCall).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CallArgumentViewTreatsNullFixedArgumentsAsNil()
        {
            Script script = new();

            DataType? capturedType = null;
            bool? capturedIsMethodCall = null;
            CallbackFunction function = CallbackFunction.FromArgumentView(
                (_, args) =>
                {
                    capturedType = args[0].Type;
                    capturedIsMethodCall = args.IsMethodCall;
                    return LuaValue.Nil;
                }
            );

            script.CallValues(LuaValue.NewCallback(function), default(LuaValue));

            await Assert.That(capturedType).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(capturedIsMethodCall).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(-1, 1, "offset")]
        [global::TUnit.Core.Arguments(3, 0, "offset")]
        [global::TUnit.Core.Arguments(0, -1, "count")]
        [global::TUnit.Core.Arguments(1, 2, "count")]
        public async Task InvokeArgumentViewStackRejectsInvalidRanges(
            int offset,
            int count,
            string paramName
        )
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackFunction function = CallbackFunction.FromArgumentView(
                (_, _) => throw new InvalidOperationException("Callback should not run.")
            );
            List<LuaValue> args = new() { LuaValue.NewNumber(1), LuaValue.NewNumber(2) };

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                function.InvokeArgumentViewStack(context, args, offset, count)
            );

            await Assert.That(exception.ParamName).IsEqualTo(paramName).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CallLegacyCallbackTreatsNullFixedArgumentsAsNil()
        {
            Script script = new();

            DataType? capturedType = null;
            CallbackFunction function = new(
                (_, args) =>
                {
                    capturedType = args[0].Type;
                    return LuaValue.Nil;
                }
            );

            script.CallValues(LuaValue.NewCallback(function), default(LuaValue));

            await Assert.That(capturedType).IsEqualTo(DataType.Nil).ConfigureAwait(false);
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
            List<LuaValue> args = new() { LuaValue.NewNumber(41) };

            LuaValue result = function.Invoke(context, args);
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
        public async Task BindToScriptCachesPerScriptOwnedCallbackWithoutMutatingSharedCallback()
        {
            CallbackFunction shared = new((_, _) => LuaValue.Nil);
            Script firstScript = new();
            Script secondScript = new();
            object initialAdditionalData = new();
            object updatedAdditionalData = new();
            shared.AdditionalData = initialAdditionalData;

            CallbackFunction first = shared.BindToScript(firstScript);
            CallbackFunction firstAgain = shared.BindToScript(firstScript);
            CallbackFunction second = shared.BindToScript(secondScript);

            await Assert.That(shared.OwnerScript).IsNull().ConfigureAwait(false);
            await Assert
                .That(first.OwnerScript)
                .IsSameReferenceAs(firstScript)
                .ConfigureAwait(false);
            await Assert.That(firstAgain).IsSameReferenceAs(first).ConfigureAwait(false);
            await Assert
                .That(second.OwnerScript)
                .IsSameReferenceAs(secondScript)
                .ConfigureAwait(false);
            await Assert.That(second).IsNotSameReferenceAs(first).ConfigureAwait(false);
            await Assert
                .That(first.AdditionalData)
                .IsSameReferenceAs(initialAdditionalData)
                .ConfigureAwait(false);

            first.AdditionalData = updatedAdditionalData;

            await Assert
                .That(shared.AdditionalData)
                .IsSameReferenceAs(updatedAdditionalData)
                .ConfigureAwait(false);
            Assert.Throws<ScriptRuntimeException>(() =>
                first.Invoke(secondScript.CreateDynamicExecutionContext(), Array.Empty<LuaValue>())
            );
        }

        [global::TUnit.Core.Test]
        public async Task CheckCallbackSignatureHonoursVisibilityRequirement()
        {
            MethodInfo publicMethod = SampleUserData.GetPublicCallbackMethod();
            MethodInfo internalMethod = SampleUserData.GetInternalCallbackMethod();
            MethodInfo badMethod = SampleUserData.GetBadSignatureMethod();
            MethodInfo argumentViewMethod = SampleUserData.GetArgumentViewCallbackMethod();
            MethodInfo argumentViewNoContextMethod =
                SampleUserData.GetArgumentViewNoContextCallbackMethod();

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
            await Assert
                .That(CallbackFunction.CheckCallbackSignature(argumentViewMethod, true))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(CallbackFunction.CheckArgumentViewCallbackSignature(argumentViewMethod, true))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(CallbackFunction.CheckLegacyCallbackSignature(argumentViewMethod, true))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(CallbackFunction.CheckCallbackSignature(argumentViewNoContextMethod, true))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(
                    CallbackFunction.CheckArgumentViewNoContextCallbackSignature(
                        argumentViewNoContextMethod,
                        true
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(
                    CallbackFunction.CheckArgumentViewCallbackSignature(
                        argumentViewNoContextMethod,
                        true
                    )
                )
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(
                    CallbackFunction.CheckLegacyCallbackSignature(argumentViewNoContextMethod, true)
                )
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ArgumentViewCountsForwardedMultiReturnsWithoutVoidSentinel(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            List<int> receivedCounts = new();
            script.Globals["countArgs"] = LuaValue.NewCallbackView(
                script,
                args =>
                {
                    receivedCounts.Add(args.Count);
                    return LuaValue.FromNumber(args.Count);
                }
            );

            // values(0) returns zero results; its expansion previously leaked a trailing void
            // sentinel into registered argument views, inflating every forwarded count by one.
            LuaValue result = script.DoString(
                @"
local function values(m)
    if m == 0 then return end
    return m, values(m - 1)
end
local function nothing() end
return countArgs(values(5)), countArgs(nothing()), countArgs(7, 8, 9)
"
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(receivedCounts.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(receivedCounts[0]).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(receivedCounts[1]).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(receivedCounts[2]).IsEqualTo(3).ConfigureAwait(false);
        }

        private sealed class SampleUserData
        {
            private static readonly MethodInfo ValidCallbackMethodInfo = (
                (Func<ScriptExecutionContext, CallbackArguments, LuaValue>)ValidCallback
            ).Method;

            private static readonly MethodInfo ArgumentViewCallbackMethodInfo = (
                (ScriptFunctionCallbackView)ValidArgumentViewCallback
            ).Method;

            private static readonly MethodInfo ArgumentViewNoContextCallbackMethodInfo = (
                (ScriptFunctionCallbackViewNoContext)ValidArgumentViewNoContextCallback
            ).Method;

            private static readonly MethodInfo PrivateCallbackMethodInfo = (
                (Func<ScriptExecutionContext, CallbackArguments, LuaValue>)PrivateCallback
            ).Method;

            private static readonly MethodInfo BadSignatureMethodInfo = (
                (Func<ScriptExecutionContext, int, LuaValue>)BadSignature
            ).Method;

            public static int AddOne(int value)
            {
                return value + 1;
            }

            public static LuaValue ValidCallback(
                ScriptExecutionContext context,
                CallbackArguments args
            )
            {
                return LuaValue.NewNumber(args[0].Number + 1);
            }

            public static LuaValue ValidArgumentViewCallback(
                ScriptExecutionContext context,
                CallbackArgumentsView args
            )
            {
                return LuaValue.NewNumber(args[0].Number + 1);
            }

            public static LuaValue ValidArgumentViewNoContextCallback(CallbackArgumentsView args)
            {
                return LuaValue.NewNumber(args[0].Number + 1);
            }

            internal static LuaValue PrivateCallback(
                ScriptExecutionContext context,
                CallbackArguments args
            )
            {
                return LuaValue.NewNumber(args[0].Number + 1);
            }

            public static LuaValue BadSignature(ScriptExecutionContext context, int value)
            {
                return LuaValue.NewNumber(value);
            }

            public static MethodInfo GetPublicCallbackMethod()
            {
                return ValidCallbackMethodInfo;
            }

            public static MethodInfo GetArgumentViewCallbackMethod()
            {
                return ArgumentViewCallbackMethodInfo;
            }

            public static MethodInfo GetArgumentViewNoContextCallbackMethod()
            {
                return ArgumentViewNoContextCallbackMethodInfo;
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
