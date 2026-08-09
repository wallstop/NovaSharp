namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Converters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using global::NovaSharp;
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
            table.Set(1, LuaValue.NewNumber(2));

            bool handledNull = ClrToScriptConversions.TryObjectToTrivialDynValue(
                script,
                null,
                out LuaValue explicitNil
            );
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, null).Value.IsNil)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(handledNull).IsTrue().ConfigureAwait(false);
            await Assert.That(explicitNil.IsNil).IsTrue().ConfigureAwait(false);
            await Assert
                .That(ClrToScriptConversions.ObjectToDynValue(script, null).IsNil)
                .IsTrue()
                .ConfigureAwait(false);

            LuaValue dyn = LuaValue.NewNumber(5);
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, dyn).Value)
                .IsEqualTo(dyn)
                .ConfigureAwait(false);
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, true).Value.Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, "abc").Value.String)
                .IsEqualTo("abc")
                .ConfigureAwait(false);
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, 42).Value.Number)
                .IsEqualTo(42d)
                .ConfigureAwait(false);
            await Assert
                .That(ClrToScriptConversions.TryObjectToTrivialDynValue(script, table).Value.Table)
                .IsSameReferenceAs(table)
                .ConfigureAwait(false);

            bool handledUnsupported = ClrToScriptConversions.TryObjectToTrivialDynValue(
                script,
                new object(),
                out LuaValue unsupported
            );
            await Assert.That(handledUnsupported).IsFalse().ConfigureAwait(false);
            await Assert.That(unsupported.IsNil).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToTrivialDynValueUsesCachedScalars()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();

            LuaValue trueResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, true)
                .Value;
            await Assert.That(trueResult).IsEqualTo(LuaValue.True).ConfigureAwait(false);

            LuaValue falseResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, false)
                .Value;
            await Assert.That(falseResult).IsEqualTo(LuaValue.False).ConfigureAwait(false);

            LuaValue integerResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, 42)
                .Value;
            await Assert
                .That(integerResult)
                .IsEqualTo(LuaValue.FromInteger(42))
                .ConfigureAwait(false);
            await Assert.That(integerResult.IsInteger).IsTrue().ConfigureAwait(false);

            LuaValue negativeIntegerResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, -1)
                .Value;
            await Assert
                .That(negativeIntegerResult)
                .IsEqualTo(LuaValue.FromInteger(-1))
                .ConfigureAwait(false);
            await Assert.That(negativeIntegerResult.IsInteger).IsTrue().ConfigureAwait(false);

            LuaValue wholeDoubleResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, 1d)
                .Value;
            await Assert
                .That(wholeDoubleResult)
                .IsEqualTo(LuaValue.FromNumber(1d))
                .ConfigureAwait(false);
            await Assert.That(wholeDoubleResult.IsInteger).IsTrue().ConfigureAwait(false);

            LuaValue fractionalDoubleResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, 3.5d)
                .Value;
            await Assert.That(fractionalDoubleResult.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(fractionalDoubleResult.Number).IsEqualTo(3.5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValueUsesCustomConverters()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                    registry.SetClrToScriptTryConversion<CustomValue>(
                        (Script _, CustomValue value, out LuaValue converted) =>
                        {
                            if (value.Name == "nil")
                            {
                                converted = LuaValue.Nil;
                                return true;
                            }

                            if (value.Name == "void")
                            {
                                converted = LuaValue.Void;
                                return true;
                            }

                            if (value.Name == "decline")
                            {
                                converted = LuaValue.NewString("ignored");
                                return false;
                            }

                            converted = LuaValue.NewString("converted");
                            return true;
                        }
                    )
            );
            Script script = new();

            bool handled = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                new CustomValue("converted"),
                out LuaValue result
            );
            bool handledNil = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                new CustomValue("nil"),
                out LuaValue nilResult
            );
            bool handledVoid = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                new CustomValue("void"),
                out LuaValue voidResult
            );
            bool declined = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                new CustomValue("decline"),
                out LuaValue declinedResult
            );

            await Assert.That(handled).IsTrue().ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("converted").ConfigureAwait(false);
            await Assert.That(handledNil).IsTrue().ConfigureAwait(false);
            await Assert.That(nilResult.IsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(handledVoid).IsTrue().ConfigureAwait(false);
            await Assert.That(voidResult.IsVoid()).IsTrue().ConfigureAwait(false);
            await Assert
                .That(ClrToScriptConversions.ObjectToDynValue(script, new CustomValue("nil")).IsNil)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(
                    ClrToScriptConversions
                        .ObjectToDynValue(script, new CustomValue("void"))
                        .IsVoid()
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(declined).IsFalse().ConfigureAwait(false);
            await Assert.That(declinedResult.IsNil).IsTrue().ConfigureAwait(false);
            await Assert
                .That(
                    ClrToScriptConversions.TryObjectToSimpleDynValue(
                        script,
                        new CustomValue("decline")
                    )
                )
                .IsNull()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValuePrefersPrimitiveCustomConverters()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                    registry.SetClrToScriptCustomConversion<int>(
                        (_, value) => LuaValue.NewString("custom:" + value)
                    )
            );
            Script script = new();

            LuaValue result = ClrToScriptConversions.TryObjectToSimpleDynValue(script, 42).Value;

            await Assert.That(result.String).IsEqualTo("custom:42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValueUsesCachedScalars()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();

            LuaValue trueResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, true)
                .Value;
            await Assert.That(trueResult).IsEqualTo(LuaValue.True).ConfigureAwait(false);

            LuaValue integerResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, 42)
                .Value;
            await Assert
                .That(integerResult)
                .IsEqualTo(LuaValue.FromInteger(42))
                .ConfigureAwait(false);
            await Assert.That(integerResult.IsInteger).IsTrue().ConfigureAwait(false);

            LuaValue wholeDoubleResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, 1d)
                .Value;
            await Assert
                .That(wholeDoubleResult)
                .IsEqualTo(LuaValue.FromNumber(1d))
                .ConfigureAwait(false);
            await Assert.That(wholeDoubleResult.IsInteger).IsTrue().ConfigureAwait(false);

            LuaValue fractionalDoubleResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, 3.5d)
                .Value;
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
            LuaValue closureValue = script.DoString("return function(a) return a end");

            LuaValue closureResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, closureValue.Function)
                .Value;
            await Assert
                .That(closureResult.Type)
                .IsEqualTo(DataType.Function)
                .ConfigureAwait(false);
            await Assert
                .That(closureResult.Function)
                .IsSameReferenceAs(closureValue.Function)
                .ConfigureAwait(false);

            LuaValue repeatedClosureResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, closureValue.Function)
                .Value;
            await Assert
                .That(repeatedClosureResult.Function)
                .IsSameReferenceAs(closureResult.Function)
                .ConfigureAwait(false);

            CallbackFunction callback = new((_, _) => LuaValue.NewNumber(7));
            LuaValue callbackResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, callback)
                .Value;
            await Assert
                .That(callbackResult.Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);

            LuaValue repeatedCallbackResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, callback)
                .Value;
            await Assert
                .That(repeatedCallbackResult.Callback)
                .IsSameReferenceAs(callbackResult.Callback)
                .ConfigureAwait(false);

            int? callbackViewCount = null;
            ScriptFunctionCallbackView callbackView = (_, args) =>
            {
                callbackViewCount = args.Count;
                return LuaValue.NewNumber(args.Count);
            };
            LuaValue callbackViewResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, callbackView)
                .Value;
            await Assert
                .That(callbackViewResult.Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);

            LuaValue callbackViewReturn = script.Call(
                callbackViewResult,
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2)
            );
            await Assert.That(callbackViewReturn.Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(callbackViewCount).IsEqualTo(2).ConfigureAwait(false);

            int? noContextCallbackViewCount = null;
            ScriptFunctionCallbackViewNoContext noContextCallbackView = args =>
            {
                noContextCallbackViewCount = args.Count;
                return LuaValue.NewNumber(args.Count);
            };
            LuaValue noContextCallbackViewResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, noContextCallbackView)
                .Value;
            await Assert
                .That(noContextCallbackViewResult.Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);

            LuaValue noContextCallbackViewReturn = script.Call(
                noContextCallbackViewResult,
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3)
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

            LuaValue userData = ClrToScriptConversions.ObjectToDynValue(script, instance);
            await Assert.That(userData.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);

            LuaValue staticUserData = ClrToScriptConversions.ObjectToDynValue(
                script,
                typeof(SampleUserData)
            );
            await Assert
                .That(staticUserData.Type)
                .IsEqualTo(DataType.UserData)
                .ConfigureAwait(false);

            LuaValue enumValue = ClrToScriptConversions.ObjectToDynValue(script, DayOfWeek.Friday);
            await Assert
                .That(enumValue.Number)
                .IsEqualTo((double)DayOfWeek.Friday)
                .ConfigureAwait(false);

            Func<int> simpleDelegate = () => 5;
            LuaValue delegateValue = ClrToScriptConversions.ObjectToDynValue(
                script,
                simpleDelegate
            );
            await Assert
                .That(delegateValue.Type)
                .IsEqualTo(DataType.ClrFunction)
                .ConfigureAwait(false);

            MethodInfo method = StaticClrCallbackMethodInfo;
            LuaValue methodValue = ClrToScriptConversions.ObjectToDynValue(script, method);
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

            LuaValue listValue = ClrToScriptConversions.ObjectToDynValue(script, list);
            await Assert.That(listValue.Table.Get(1).Number).IsEqualTo(1d).ConfigureAwait(false);

            LuaValue dictValue = ClrToScriptConversions.ObjectToDynValue(script, dictionary);
            await Assert
                .That(dictValue.Table.Get("key").Number)
                .IsEqualTo(3d)
                .ConfigureAwait(false);

            IEnumerable enumerable = YieldStrings();
            LuaValue enumerableValue = ClrToScriptConversions.ObjectToDynValue(script, enumerable);
            await Assert.That(enumerableValue.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);

            IEnumerator enumerator = YieldStrings().GetEnumerator();
            LuaValue iteratorTuple = ClrToScriptConversions.ObjectToDynValue(script, enumerator);
            await Assert.That(iteratorTuple.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);

            bool handledEnumerator = ClrToScriptConversions.TryEnumerationToDynValue(
                script,
                enumerator,
                out LuaValue explicitIteratorTuple
            );
            bool handledObject = ClrToScriptConversions.TryEnumerationToDynValue(
                script,
                new object(),
                out LuaValue missingIterator
            );
            await Assert.That(handledEnumerator).IsTrue().ConfigureAwait(false);
            await Assert
                .That(explicitIteratorTuple.Type)
                .IsEqualTo(DataType.Tuple)
                .ConfigureAwait(false);
            await Assert.That(handledObject).IsFalse().ConfigureAwait(false);
            await Assert.That(missingIterator.IsNil).IsTrue().ConfigureAwait(false);
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
            CallbackFunction function = new((_, _) => LuaValue.NewNumber(7));

            LuaValue result = ClrToScriptConversions.ObjectToDynValue(script, function);
            LuaValue repeatedResult = ClrToScriptConversions.ObjectToDynValue(script, function);

            await Assert.That(result.Type).IsEqualTo(DataType.ClrFunction).ConfigureAwait(false);
            await Assert
                .That(repeatedResult.Callback)
                .IsSameReferenceAs(result.Callback)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToTrivialDynValuePreservesIntegerSubtype()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();

            // Test various integer types
            LuaValue intResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, 42)
                .Value;
            await Assert.That(intResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(intResult.LuaNumber.AsInteger).IsEqualTo(42L).ConfigureAwait(false);

            LuaValue longResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, 9007199254740993L)
                .Value;
            await Assert.That(longResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(longResult.LuaNumber.AsInteger)
                .IsEqualTo(9007199254740993L)
                .ConfigureAwait(false);

            LuaValue byteResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, (byte)255)
                .Value;
            await Assert.That(byteResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(byteResult.LuaNumber.AsInteger).IsEqualTo(255L).ConfigureAwait(false);

            LuaValue shortResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, (short)1000)
                .Value;
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
            LuaValue floatResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, 3.14f)
                .Value;
            await Assert.That(floatResult.IsInteger).IsFalse().ConfigureAwait(false);

            LuaValue doubleResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, 3.14159)
                .Value;
            await Assert.That(doubleResult.IsInteger).IsFalse().ConfigureAwait(false);

            LuaValue decimalResult = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, 3.14m)
                .Value;
            await Assert.That(decimalResult.IsInteger).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValuePreservesIntegerSubtype()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();

            // Large integer beyond double precision
            LuaValue longResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, 9007199254740993L)
                .Value;
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
            LuaValue longResult = ClrToScriptConversions.ObjectToDynValue(
                script,
                9007199254740993L
            );
            await Assert.That(longResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(longResult.LuaNumber.AsInteger)
                .IsEqualTo(9007199254740993L)
                .ConfigureAwait(false);

            // Small int
            LuaValue intResult = ClrToScriptConversions.ObjectToDynValue(script, 42);
            await Assert.That(intResult.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(intResult.LuaNumber.AsInteger).IsEqualTo(42L).ConfigureAwait(false);
        }

        public static LuaValue StaticClrCallback(ScriptExecutionContext ctx, CallbackArguments args)
        {
            return LuaValue.NewNumber(42);
        }

        private static readonly MethodInfo StaticClrCallbackMethodInfo = (
            (Func<ScriptExecutionContext, CallbackArguments, LuaValue>)StaticClrCallback
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
