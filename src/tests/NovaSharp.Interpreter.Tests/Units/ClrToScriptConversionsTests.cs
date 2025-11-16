namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Converters;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ClrToScriptConversionsTests
    {
        [SetUp]
        public void SetUp()
        {
            Script.GlobalOptions.CustomConverters.Clear();
        }

        [Test]
        public void TryObjectToTrivialDynValueCoversPrimitives()
        {
            Script script = new();
            Table table = new(script);
            table.Set(1, DynValue.NewNumber(2));

            Assert.Multiple(() =>
            {
                Assert.That(
                    ClrToScriptConversions.TryObjectToTrivialDynValue(script, null).IsNil(),
                    Is.True
                );
                DynValue dyn = DynValue.NewNumber(5);
                Assert.That(
                    ClrToScriptConversions.TryObjectToTrivialDynValue(script, dyn),
                    Is.SameAs(dyn)
                );
                Assert.That(
                    ClrToScriptConversions.TryObjectToTrivialDynValue(script, true).Boolean,
                    Is.True
                );
                Assert.That(
                    ClrToScriptConversions.TryObjectToTrivialDynValue(script, "abc").String,
                    Is.EqualTo("abc")
                );
                Assert.That(
                    ClrToScriptConversions.TryObjectToTrivialDynValue(script, 42).Number,
                    Is.EqualTo(42d)
                );
                Assert.That(
                    ClrToScriptConversions.TryObjectToTrivialDynValue(script, table).Table,
                    Is.SameAs(table)
                );
            });
        }

        [Test]
        public void TryObjectToSimpleDynValueUsesCustomConverters()
        {
            Script script = new();
            Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<CustomValue>(
                (s, value) => DynValue.NewString(value.Name)
            );

            DynValue result = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                new CustomValue("converted")
            );

            Assert.That(result.String, Is.EqualTo("converted"));
        }

        [Test]
        public void TryObjectToSimpleDynValueHandlesClosuresCallbacksAndDelegates()
        {
            Script script = new();
            DynValue closureValue = script.DoString("return function(a) return a end");

            DynValue closureResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                closureValue.Function
            );
            Assert.That(closureResult.Type, Is.EqualTo(DataType.Function));

            CallbackFunction callback = new((_, _) => DynValue.NewNumber(7));
            DynValue callbackResult = ClrToScriptConversions.TryObjectToSimpleDynValue(
                script,
                callback
            );
            Assert.That(callbackResult.Type, Is.EqualTo(DataType.ClrFunction));

        }

        [Test]
        public void ObjectToDynValueHandlesUserDataTypesEnumsAndDelegates()
        {
            RegisterSampleUserData();
            Script script = new();
            SampleUserData instance = new();

            DynValue userData = ClrToScriptConversions.ObjectToDynValue(script, instance);
            Assert.That(userData.Type, Is.EqualTo(DataType.UserData));

            DynValue staticUserData = ClrToScriptConversions.ObjectToDynValue(
                script,
                typeof(SampleUserData)
            );
            Assert.That(staticUserData.Type, Is.EqualTo(DataType.UserData));

            DynValue enumValue = ClrToScriptConversions.ObjectToDynValue(script, DayOfWeek.Friday);
            Assert.That(enumValue.Number, Is.EqualTo((double)DayOfWeek.Friday));

            Func<int> simpleDelegate = () => 5;
            DynValue delegateValue = ClrToScriptConversions.ObjectToDynValue(
                script,
                simpleDelegate
            );
            Assert.That(delegateValue.Type, Is.EqualTo(DataType.ClrFunction));

            MethodInfo method = typeof(ClrToScriptConversionsTests)
                .GetMethod(
                    nameof(StaticClrCallback),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                );
            DynValue methodValue = ClrToScriptConversions.ObjectToDynValue(script, method);
            Assert.That(methodValue.Type, Is.EqualTo(DataType.ClrFunction));
        }

        [Test]
        public void ObjectToDynValueConvertsCollectionsAndEnumerables()
        {
            Script script = new();
            List<int> list = new() { 1, 2 };
            Dictionary<string, int> dictionary = new() { ["key"] = 3 };

            DynValue listValue = ClrToScriptConversions.ObjectToDynValue(script, list);
            Assert.That(listValue.Table.Get(1).Number, Is.EqualTo(1));

            DynValue dictValue = ClrToScriptConversions.ObjectToDynValue(script, dictionary);
            Assert.That(dictValue.Table.Get("key").Number, Is.EqualTo(3));

            IEnumerable enumerable = YieldStrings();
            DynValue enumerableValue = ClrToScriptConversions.ObjectToDynValue(script, enumerable);
            Assert.That(enumerableValue.Type, Is.EqualTo(DataType.Tuple));

            IEnumerator enumerator = YieldStrings().GetEnumerator();
            DynValue iteratorTuple = ClrToScriptConversions.ObjectToDynValue(script, enumerator);
            Assert.That(iteratorTuple.Type, Is.EqualTo(DataType.Tuple));
        }

        [Test]
        public void ObjectToDynValueThrowsWhenConversionFails()
        {
            Script script = new();
            Assert.That(
                () => ClrToScriptConversions.ObjectToDynValue(script, new object()),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("cannot convert clr type")
            );
        }

        public static DynValue StaticClrCallback(ScriptExecutionContext ctx, CallbackArguments args)
        {
            return DynValue.NewNumber(42);
        }

        private static void RegisterSampleUserData()
        {
            if (!UserData.IsTypeRegistered(typeof(SampleUserData)))
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
