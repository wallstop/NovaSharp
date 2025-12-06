namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
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
    public sealed class ClrToScriptConversionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TryObjectToTrivialDynValueCoversPrimitives()
        {
            Script script = new();
            Table table = new(script);
            table.Set(1, DynValue.NewNumber(2));

            DynValue nilValue = ClrToScriptConversions.TryObjectToTrivialDynValue(script, null);
            await Assert.That(nilValue.IsNil()).IsTrue();

            DynValue dyn = DynValue.NewNumber(5);
            DynValue passthrough = ClrToScriptConversions.TryObjectToTrivialDynValue(script, dyn);
            await Assert.That(ReferenceEquals(passthrough, dyn)).IsTrue();

            DynValue booleanValue = ClrToScriptConversions.TryObjectToTrivialDynValue(script, true);
            await Assert.That(booleanValue.Boolean).IsTrue();

            DynValue stringValue = ClrToScriptConversions.TryObjectToTrivialDynValue(script, "abc");
            await Assert.That(stringValue.String).IsEqualTo("abc");

            DynValue numberValue = ClrToScriptConversions.TryObjectToTrivialDynValue(script, 42);
            await Assert.That(numberValue.Number).IsEqualTo(42d);

            DynValue tableValue = ClrToScriptConversions.TryObjectToTrivialDynValue(script, table);
            await Assert.That(ReferenceEquals(tableValue.Table, table)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValueUsesCustomConverters()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                    registry.SetClrToScriptCustomConversion<CustomValue>(
                        (script, value) => DynValue.NewString(value.Name)
                    )
            );

            Script script = new();
            DynValue result = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                new CustomValue("converted")
            );
            await Assert.That(result.String).IsEqualTo("converted");
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValueHandlesClosuresCallbacksAndDelegates()
        {
            Script script = new();
            DynValue closureValue = script.DoString("return function(a) return a end");

            DynValue closureResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                closureValue.Function
            );
            await Assert.That(closureResult.Type).IsEqualTo(DataType.Function);

            CallbackFunction callback = new((_, _) => DynValue.NewNumber(7));
            DynValue callbackResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                callback
            );
            await Assert.That(callbackResult.Type).IsEqualTo(DataType.ClrFunction);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToDynValueHandlesUserDataTypesEnumsAndDelegates()
        {
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<SampleUserData>(ensureUnregistered: true);
            registrationScope.RegisterType<SampleUserData>();
            Script script = new();
            SampleUserData instance = new();

            DynValue userData = ClrToScriptConversions.ObjectToDynValue(script, instance);
            await Assert.That(userData.Type).IsEqualTo(DataType.UserData);

            DynValue staticUserData = ClrToScriptConversions.ObjectToDynValue(
                script,
                typeof(SampleUserData)
            );
            await Assert.That(staticUserData.Type).IsEqualTo(DataType.UserData);

            DynValue enumValue = ClrToScriptConversions.ObjectToDynValue(script, DayOfWeek.Friday);
            await Assert.That(enumValue.Number).IsEqualTo((double)DayOfWeek.Friday);

            Func<int> simpleDelegate = () => 5;
            DynValue delegateValue = ClrToScriptConversions.ObjectToDynValue(
                script,
                simpleDelegate
            );
            await Assert.That(delegateValue.Type).IsEqualTo(DataType.ClrFunction);

            DynValue methodValue = ClrToScriptConversions.ObjectToDynValue(
                script,
                StaticClrCallbackMethodInfo
            );
            await Assert.That(methodValue.Type).IsEqualTo(DataType.ClrFunction);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToDynValueConvertsCollectionsAndEnumerables()
        {
            Script script = new();
            List<int> list = new() { 1, 2 };
            Dictionary<string, int> dictionary = new() { ["key"] = 3 };

            DynValue listValue = ClrToScriptConversions.ObjectToDynValue(script, list);
            await Assert.That(listValue.Table.Get(1).Number).IsEqualTo(1);

            DynValue dictValue = ClrToScriptConversions.ObjectToDynValue(script, dictionary);
            await Assert.That(dictValue.Table.Get("key").Number).IsEqualTo(3);

            IEnumerable enumerable = YieldStrings();
            DynValue enumerableValue = ClrToScriptConversions.ObjectToDynValue(script, enumerable);
            await Assert.That(enumerableValue.Type).IsEqualTo(DataType.Tuple);

            IEnumerator enumerator = YieldStrings().GetEnumerator();
            DynValue iteratorTuple = ClrToScriptConversions.ObjectToDynValue(script, enumerator);
            await Assert.That(iteratorTuple.Type).IsEqualTo(DataType.Tuple);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToDynValueThrowsWhenConversionFails()
        {
            Script script = new();

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                ClrToScriptConversions.ObjectToDynValue(script, new object())
            );

            await Assert
                .That(
                    exception.Message.Contains("cannot convert clr type", StringComparison.Ordinal)
                )
                .IsTrue();
        }

        public static DynValue StaticClrCallback(ScriptExecutionContext ctx, CallbackArguments args)
        {
            return DynValue.NewNumber(42);
        }

        private static readonly MethodInfo StaticClrCallbackMethodInfo = (
            (Func<ScriptExecutionContext, CallbackArguments, DynValue>)StaticClrCallback
        ).Method;

        private static IEnumerable<string> YieldStrings()
        {
            yield return "a";
            yield return "b";
        }

        private sealed class SampleUserData { }

        private sealed record CustomValue(string Name);

        private static TException ExpectException<TException>(Func<object> factory)
            where TException : Exception
        {
            try
            {
                factory();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }
}
