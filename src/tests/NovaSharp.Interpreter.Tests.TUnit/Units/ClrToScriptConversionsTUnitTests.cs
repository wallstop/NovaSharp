namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Converters;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

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
        public async Task TryObjectToSimpleDynValueUsesCustomConverters()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();
            Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<CustomValue>(
                (_, _) => DynValue.NewString("converted")
            );

            DynValue result = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                new CustomValue("converted")
            );

            await Assert.That(result.String).IsEqualTo("converted").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValueHandlesClosuresCallbacksAndDelegates()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            Script script = new();
            DynValue closureValue = script.DoString("return function(a) return a end");

            DynValue closureResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                closureValue.Function
            );
            await Assert
                .That(closureResult.Type)
                .IsEqualTo(DataType.Function)
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
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToDynValueHandlesUserDataTypesEnumsAndDelegates()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear();
            RegisterSampleUserData();
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

            await Assert.That(result.Type).IsEqualTo(DataType.ClrFunction).ConfigureAwait(false);
        }

        public static DynValue StaticClrCallback(ScriptExecutionContext ctx, CallbackArguments args)
        {
            return DynValue.NewNumber(42);
        }

        private static readonly MethodInfo StaticClrCallbackMethodInfo = (
            (Func<ScriptExecutionContext, CallbackArguments, DynValue>)StaticClrCallback
        ).Method;

        private static void RegisterSampleUserData()
        {
            if (!UserData.IsTypeRegistered<SampleUserData>())
            {
                UserData.RegisterType<SampleUserData>();
            }
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
