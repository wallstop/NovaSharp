namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Converters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Converters;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [ScriptGlobalOptionsIsolation]
    [UserDataIsolation]
    public sealed class ClrToScriptConversionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TryObjectToTrivialDynValueCoversPrimitives()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();
            Table table = new(script);
            table.Set(1, DynValue.NewNumber(2));

            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, null).IsNil())
                .IsTrue()
                .ConfigureAwait(false);

            DynValue dyn = DynValue.NewNumber(5);
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, dyn))
                .IsSameReferenceAs(dyn)
                .ConfigureAwait(false);
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, true).Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, "abc").String)
                .IsEqualTo("abc")
                .ConfigureAwait(false);
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, 42).Number)
                .IsEqualTo(42d)
                .ConfigureAwait(false);
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, table).Table)
                .IsSameReferenceAs(table)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToTrivialDynValueUsesCachedScalars()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();

            DynValue trueResult = ClrToScriptConversions.TryObjectToTrivialDynValue(script, true);
            await Assert.That(trueResult).IsSameReferenceAs(DynValue.True).ConfigureAwait(false);

            DynValue falseResult = ClrToScriptConversions.TryObjectToTrivialDynValue(script, false);
            await Assert.That(falseResult).IsSameReferenceAs(DynValue.False).ConfigureAwait(false);

            DynValue integerResult = ClrToScriptConversions.TryObjectToTrivialDynValue(script, 42);
            await Assert
                .That(integerResult)
                .IsSameReferenceAs(DynValue.FromInteger(42))
                .ConfigureAwait(false);
            await Assert.That(integerResult.IsInteger).IsTrue().ConfigureAwait(false);

            DynValue negativeIntegerResult = ClrToScriptConversions.TryObjectToTrivialDynValue(
                script,
                -1
            );
            await Assert
                .That(negativeIntegerResult)
                .IsSameReferenceAs(DynValue.FromInteger(-1))
                .ConfigureAwait(false);
            await Assert.That(negativeIntegerResult.IsInteger).IsTrue().ConfigureAwait(false);

            DynValue wholeDoubleResult = ClrToScriptConversions.TryObjectToTrivialDynValue(
                script,
                1d
            );
            await Assert
                .That(wholeDoubleResult)
                .IsSameReferenceAs(DynValue.FromNumber(1d))
                .ConfigureAwait(false);
            await Assert.That(wholeDoubleResult.IsInteger).IsTrue().ConfigureAwait(false);

            DynValue fractionalDoubleResult = ClrToScriptConversions.TryObjectToTrivialDynValue(
                script,
                3.5d
            );
            await Assert.That(fractionalDoubleResult.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(fractionalDoubleResult.Number).IsEqualTo(3.5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValueUsesCustomConverters()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                    registry.SetClrToScriptCustomConversion<CustomValue>(
                        (_, _) => DynValue.NewString("converted")
                    )
            );
            Script script = new();

            DynValue result = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                new CustomValue("converted")
            );

            await Assert.That(result.String).IsEqualTo("converted").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValuePrefersPrimitiveCustomConverters()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                    registry.SetClrToScriptCustomConversion<int>(
                        (_, value) => DynValue.NewString("custom:" + value)
                    )
            );
            Script script = new();

            DynValue result = ClrToScriptConversions.TryObjectToSimpleDynValue(script, 42);

            await Assert.That(result.String).IsEqualTo("custom:42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValueUsesCachedScalars()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();

            DynValue trueResult = ClrToScriptConversions.TryObjectToSimpleDynValue(script, true);
            await Assert.That(trueResult).IsSameReferenceAs(DynValue.True).ConfigureAwait(false);

            DynValue integerResult = ClrToScriptConversions.TryObjectToSimpleDynValue(script, 42);
            await Assert
                .That(integerResult)
                .IsSameReferenceAs(DynValue.FromInteger(42))
                .ConfigureAwait(false);
            await Assert.That(integerResult.IsInteger).IsTrue().ConfigureAwait(false);

            DynValue wholeDoubleResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                1d
            );
            await Assert
                .That(wholeDoubleResult)
                .IsSameReferenceAs(DynValue.FromNumber(1d))
                .ConfigureAwait(false);
            await Assert.That(wholeDoubleResult.IsInteger).IsTrue().ConfigureAwait(false);

            DynValue fractionalDoubleResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                3.5d
            );
            await Assert.That(fractionalDoubleResult.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(fractionalDoubleResult.Number).IsEqualTo(3.5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TryObjectToSimpleDynValueHandlesClosuresCallbacksAndDelegates(
            LuaCompatibilityVersion version
        )
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new(version);
            DynValue closureValue = script.DoString("return function(a) return a end");

            DynValue closureResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                closureValue.Function
            );
            await Assert
                .That(closureResult.Type)
                .IsEqualTo(DataType.Function)
                .ConfigureAwait(false);
            await Assert
                .That(closureResult.Function)
                .IsSameReferenceAs(closureValue.Function)
                .ConfigureAwait(false);

            DynValue repeatedClosureResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                closureValue.Function
            );
            await Assert
                .That(repeatedClosureResult)
                .IsSameReferenceAs(closureResult)
                .ConfigureAwait(false);

            CallbackFunction callback = new((_, _) => DynValue.NewNumber(7));
            DynValue callbackResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                callback
            );
            await Assert
                .That(callbackResult.Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);

            DynValue repeatedCallbackResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                callback
            );
            await Assert
                .That(repeatedCallbackResult)
                .IsSameReferenceAs(callbackResult)
                .ConfigureAwait(false);

            int? callbackViewCount = null;
            ScriptFunctionCallbackView callbackView = (_, args) =>
            {
                callbackViewCount = args.Count;
                return DynValue.NewNumber(args.Count);
            };
            DynValue callbackViewResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                callbackView
            );
            await Assert
                .That(callbackViewResult.Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);

            DynValue callbackViewReturn = script.Call(
                callbackViewResult,
                DynValue.NewNumber(1),
                DynValue.NewNumber(2)
            );
            await Assert.That(callbackViewReturn.Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(callbackViewCount).IsEqualTo(2).ConfigureAwait(false);

            int? noContextCallbackViewCount = null;
            ScriptFunctionCallbackViewNoContext noContextCallbackView = args =>
            {
                noContextCallbackViewCount = args.Count;
                return DynValue.NewNumber(args.Count);
            };
            DynValue noContextCallbackViewResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                noContextCallbackView
            );
            await Assert
                .That(noContextCallbackViewResult.Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);

            DynValue noContextCallbackViewReturn = script.Call(
                noContextCallbackViewResult,
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3)
            );
            await Assert
                .That(noContextCallbackViewReturn.Number)
                .IsEqualTo(3d)
                .ConfigureAwait(false);
            await Assert.That(noContextCallbackViewCount).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToDynValueHandlesUserDataTypesEnumsAndDelegates()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            using UserDataRegistrationScope registrationScope = RegisterSampleUserData();
            Script script = new();
            SampleUserData instance = new();

            DynValue userData = ClrToScriptConversions.ObjectToDynValue(script, instance);
            await Assert.That(userData.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);

            DynValue staticUserData = ClrToScriptConversions.ObjectToDynValue(
                script,
                typeof(SampleUserData)
            );
            await Assert
                .That(staticUserData.Type)
                .IsEqualTo(DataType.UserData)
                .ConfigureAwait(false);

            DynValue enumValue = ClrToScriptConversions.ObjectToDynValue(script, DayOfWeek.Friday);
            await Assert
                .That(enumValue.Number)
                .IsEqualTo((double)DayOfWeek.Friday)
                .ConfigureAwait(false);

            Func<int> simpleDelegate = () => 5;
            DynValue delegateValue = ClrToScriptConversions.ObjectToDynValue(
                script,
                simpleDelegate
            );
            await Assert
                .That(delegateValue.Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);

            MethodInfo method = StaticClrCallbackMethodInfo;
            DynValue methodValue = ClrToScriptConversions.ObjectToDynValue(script, method);
            await Assert
                .That(methodValue.Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToDynValueConvertsCollectionsAndEnumerables()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();
            List<int> list = new() { 1, 2 };
            Dictionary<string, int> dictionary = new() { ["key"] = 3 };

            DynValue listValue = ClrToScriptConversions.ObjectToDynValue(script, list);
            await Assert.That(listValue.Table.Get(1).Number).IsEqualTo(1d).ConfigureAwait(false);

            DynValue dictValue = ClrToScriptConversions.ObjectToDynValue(script, dictionary);
            await Assert
                .That(dictValue.Table.Get("key").Number)
                .IsEqualTo(3d)
                .ConfigureAwait(false);

            IEnumerable enumerable = YieldStrings();
            DynValue enumerableValue = ClrToScriptConversions.ObjectToDynValue(script, enumerable);
            await Assert.That(enumerableValue.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);

            IEnumerator enumerator = YieldStrings().GetEnumerator();
            DynValue iteratorTuple = ClrToScriptConversions.ObjectToDynValue(script, enumerator);
            await Assert.That(iteratorTuple.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToDynValueThrowsWhenConversionFails()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                ClrToScriptConversions.ObjectToDynValue(script, new object())
            );

            await Assert
                .That(exception.Message)
                .Contains("cannot convert clr type")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToDynValueUsesCallbackFunction()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();
            CallbackFunction function = new((_, _) => DynValue.NewNumber(7));

            DynValue result = ClrToScriptConversions.ObjectToDynValue(script, function);
            DynValue repeatedResult = ClrToScriptConversions.ObjectToDynValue(script, function);

            await Assert.That(result.Type).IsEqualTo(DataType.ClrFunction).ConfigureAwait(false);
            await Assert.That(repeatedResult).IsSameReferenceAs(result).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToTrivialDynValuePreservesIntegerSubtype()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();

            // Test various integer types
            DynValue intResult = ClrToScriptConversions.TryObjectToTrivialDynValue(script, 42);
            await Assert.That(intResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(intResult.LuaNumber.AsInteger).IsEqualTo(42L).ConfigureAwait(false);

            DynValue longResult = ClrToScriptConversions.TryObjectToTrivialDynValue(
                script,
                9007199254740993L
            );
            await Assert.That(longResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(longResult.LuaNumber.AsInteger)
                .IsEqualTo(9007199254740993L)
                .ConfigureAwait(false);

            DynValue byteResult = ClrToScriptConversions.TryObjectToTrivialDynValue(
                script,
                (byte)255
            );
            await Assert.That(byteResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(byteResult.LuaNumber.AsInteger).IsEqualTo(255L).ConfigureAwait(false);

            DynValue shortResult = ClrToScriptConversions.TryObjectToTrivialDynValue(
                script,
                (short)1000
            );
            await Assert.That(shortResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(shortResult.LuaNumber.AsInteger)
                .IsEqualTo(1000L)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToTrivialDynValuePreservesFloatSubtype()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();

            // Test float types - should NOT be integers
            DynValue floatResult = ClrToScriptConversions.TryObjectToTrivialDynValue(script, 3.14f);
            await Assert.That(floatResult.IsInteger).IsFalse().ConfigureAwait(false);

            DynValue doubleResult = ClrToScriptConversions.TryObjectToTrivialDynValue(
                script,
                3.14159
            );
            await Assert.That(doubleResult.IsInteger).IsFalse().ConfigureAwait(false);

            DynValue decimalResult = ClrToScriptConversions.TryObjectToTrivialDynValue(
                script,
                3.14m
            );
            await Assert.That(decimalResult.IsInteger).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValuePreservesIntegerSubtype()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();

            // Large integer beyond double precision
            DynValue longResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                9007199254740993L
            );
            await Assert.That(longResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(longResult.LuaNumber.AsInteger)
                .IsEqualTo(9007199254740993L)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToDynValuePreservesIntegerSubtype()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();

            // Large integer beyond double precision
            DynValue longResult = ClrToScriptConversions.ObjectToDynValue(
                script,
                9007199254740993L
            );
            await Assert.That(longResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(longResult.LuaNumber.AsInteger)
                .IsEqualTo(9007199254740993L)
                .ConfigureAwait(false);

            // Small int
            DynValue intResult = ClrToScriptConversions.ObjectToDynValue(script, 42);
            await Assert.That(intResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(intResult.LuaNumber.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
        }

        public static DynValue StaticClrCallback(ScriptExecutionContext ctx, CallbackArguments args)
        {
            return DynValue.NewNumber(42);
        }

        private static readonly MethodInfo StaticClrCallbackMethodInfo = (
            (Func<ScriptExecutionContext, CallbackArguments, DynValue>)StaticClrCallback
        ).Method;

        private static UserDataRegistrationScope RegisterSampleUserData()
        {
            UserDataRegistrationScope scope = UserDataRegistrationScope.Track<SampleUserData>(
                ensureUnregistered: true
            );
            scope.RegisterType<SampleUserData>();
            return scope;
        }

        private static IEnumerable<string> YieldStrings()
        {
            yield return "a";
            yield return "b";
        }

        private sealed class SampleUserData { }

        private sealed record CustomValue(string Name);
    }
}
