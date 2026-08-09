namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.Collections;
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
    using WallstopStudios.NovaSharp.Interpreter.Interop.Converters;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [ScriptGlobalOptionsIsolation]
    public sealed class ClrToScriptConversionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TryObjectToTrivialDynValueCoversPrimitives()
        {
            Script script = new();
            Table table = new(script);
            table.Set(1, LuaValue.NewNumber(2));

            LuaValue nilValue = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, null)
                .Value;
            await Assert.That(nilValue.IsNil).IsTrue();

            LuaValue dyn = LuaValue.NewNumber(5);
            LuaValue passthrough = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, dyn)
                .Value;
            await Assert.That(passthrough).IsEqualTo(dyn);

            LuaValue booleanValue = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, true)
                .Value;
            await Assert.That(booleanValue.Boolean).IsTrue();

            LuaValue stringValue = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, "abc")
                .Value;
            await Assert.That(stringValue.String).IsEqualTo("abc");

            LuaValue numberValue = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, 42)
                .Value;
            await Assert.That(numberValue.Number).IsEqualTo(42d);

            LuaValue tableValue = ClrToScriptConversions
                .TryObjectToTrivialDynValue(script, table)
                .Value;
            await Assert.That(ReferenceEquals(tableValue.Table, table)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task TryObjectToSimpleDynValueUsesCustomConverters()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                    registry.SetClrToScriptCustomConversion<CustomValue>(
                        (script, value) => LuaValue.NewString(value.Name)
                    )
            );

            Script script = new();
            LuaValue result = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, new CustomValue("converted"))
                .Value;
            await Assert.That(result.String).IsEqualTo("converted");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TryObjectToSimpleDynValueHandlesClosuresCallbacksAndDelegates(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue closureValue = script.DoString("return function(a) return a end");

            LuaValue closureResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, closureValue.Function)
                .Value;
            await Assert.That(closureResult.Type).IsEqualTo(DataType.Function);

            CallbackFunction callback = new((_, _) => LuaValue.NewNumber(7));
            LuaValue callbackResult = ClrToScriptConversions
                .TryObjectToSimpleDynValue(script, callback)
                .Value;
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

            LuaValue userData = ClrToScriptConversions.ObjectToDynValue(script, instance);
            await Assert.That(userData.Type).IsEqualTo(DataType.UserData);

            LuaValue staticUserData = ClrToScriptConversions.ObjectToDynValue(
                script,
                typeof(SampleUserData)
            );
            await Assert.That(staticUserData.Type).IsEqualTo(DataType.UserData);

            LuaValue enumValue = ClrToScriptConversions.ObjectToDynValue(script, DayOfWeek.Friday);
            await Assert.That(enumValue.Number).IsEqualTo((double)DayOfWeek.Friday);

            Func<int> simpleDelegate = () => 5;
            LuaValue delegateValue = ClrToScriptConversions.ObjectToDynValue(
                script,
                simpleDelegate
            );
            await Assert.That(delegateValue.Type).IsEqualTo(DataType.ClrFunction);

            LuaValue methodValue = ClrToScriptConversions.ObjectToDynValue(
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

            LuaValue listValue = ClrToScriptConversions.ObjectToDynValue(script, list);
            await Assert.That(listValue.Table.Get(1).Number).IsEqualTo(1);

            LuaValue dictValue = ClrToScriptConversions.ObjectToDynValue(script, dictionary);
            await Assert.That(dictValue.Table.Get("key").Number).IsEqualTo(3);

            IEnumerable enumerable = YieldStrings();
            LuaValue enumerableValue = ClrToScriptConversions.ObjectToDynValue(script, enumerable);
            await Assert.That(enumerableValue.Type).IsEqualTo(DataType.Tuple);

            IEnumerator enumerator = YieldStrings().GetEnumerator();
            LuaValue iteratorTuple = ClrToScriptConversions.ObjectToDynValue(script, enumerator);
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

        public static LuaValue StaticClrCallback(ScriptExecutionContext ctx, CallbackArguments args)
        {
            return LuaValue.NewNumber(42);
        }

        private static readonly MethodInfo StaticClrCallbackMethodInfo = (
            (Func<ScriptExecutionContext, CallbackArguments, LuaValue>)StaticClrCallback
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
