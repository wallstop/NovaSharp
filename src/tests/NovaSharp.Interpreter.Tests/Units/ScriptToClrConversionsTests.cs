namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Converters;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptToClrConversionsTests
    {
        [TearDown]
        public void TearDown()
        {
            Script.GlobalOptions.CustomConverters.Clear();
        }

        [Test]
        public void DynValueToObjectUsesCustomConversionResult()
        {
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(
                DataType.String,
                typeof(object),
                dv => $"converted:{dv.String}"
            );

            object result = ScriptToClrConversions.DynValueToObject(DynValue.NewString("lua"));

            Assert.That(result, Is.EqualTo("converted:lua"));
        }

        [Test]
        public void DynValueToObjectReturnsNullForVoidValues()
        {
            object result = ScriptToClrConversions.DynValueToObject(DynValue.Void);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void DynValueToObjectReturnsClosureReference()
        {
            DynValue closure = CreateConstantClosure("return 41 + 1");

            object result = ScriptToClrConversions.DynValueToObject(closure);

            Assert.That(result, Is.InstanceOf<Closure>());
            Assert.That(result, Is.SameAs(closure.Function));
        }

        [Test]
        public void DynValueToObjectReturnsTableReference()
        {
            Table table = new(null);
            table.Set("language", DynValue.NewString("Lua"));
            DynValue tableValue = DynValue.NewTable(table);

            object result = ScriptToClrConversions.DynValueToObject(tableValue);

            Assert.That(result, Is.SameAs(table));
        }

        [Test]
        public void DynValueToObjectReturnsTupleArray()
        {
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewString("two");
            DynValue tupleValue = DynValue.NewTuple(first, second);

            object result = ScriptToClrConversions.DynValueToObject(tupleValue);

            Assert.That(result, Is.InstanceOf<DynValue[]>());
            DynValue[] tuple = (DynValue[])result;
            Assert.That(tuple, Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void DynValueToObjectReturnsDescriptorTypeWhenNoInstanceIsAvailable()
        {
            IUserDataDescriptor descriptor = new TestUserDataDescriptor(
                typeof(FakeUserData),
                isTypeCompatible: false,
                stringValue: "<unused>"
            );
            DynValue userData = UserData.CreateStatic(descriptor);

            object result = ScriptToClrConversions.DynValueToObject(userData);

            Assert.That(result, Is.EqualTo(descriptor.Type));
        }

        [Test]
        public void DynValueToObjectReturnsCallbackFunctionInstance()
        {
            DynValue callback = DynValue.NewCallback((ctx, args) => DynValue.NewNumber(5));

            object result = ScriptToClrConversions.DynValueToObject(callback);

            Assert.That(result, Is.InstanceOf<CallbackFunction>());
            Assert.That(result, Is.SameAs(callback.Callback));
        }

        [Test]
        public void DynValueToObjectThrowsForUnsupportedTypes()
        {
            DynValue yieldRequest = DynValue.NewYieldReq(Array.Empty<DynValue>());

            Assert.Throws<ScriptRuntimeException>(
                () => ScriptToClrConversions.DynValueToObject(yieldRequest)
            );
        }

        [Test]
        public void DynValueToObjectOfTypeReturnsDefaultForOptionalVoid()
        {
            object result = ScriptToClrConversions.DynValueToObjectOfType(
                DynValue.Void,
                typeof(int),
                defaultValue: 77,
                isOptional: true
            );

            Assert.That(result, Is.EqualTo(77));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsNilToNullable()
        {
            object result = ScriptToClrConversions.DynValueToObjectOfType(
                DynValue.Nil,
                typeof(int?),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.Null);
        }

        [Test]
        public void DynValueToObjectOfTypeReturnsDefaultForOptionalNilValueType()
        {
            object result = ScriptToClrConversions.DynValueToObjectOfType(
                DynValue.Nil,
                typeof(int),
                defaultValue: 123,
                isOptional: true
            );

            Assert.That(result, Is.EqualTo(123));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsBooleanToStringBuilder()
        {
            object result = ScriptToClrConversions.DynValueToObjectOfType(
                DynValue.NewBoolean(true),
                typeof(StringBuilder),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.TypeOf<StringBuilder>());
            Assert.That(((StringBuilder)result).ToString(), Is.EqualTo("True"));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsStringToChar()
        {
            object result = ScriptToClrConversions.DynValueToObjectOfType(
                DynValue.NewString("Nova"),
                typeof(char),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.EqualTo('N'));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsNumberToEnum()
        {
            object result = ScriptToClrConversions.DynValueToObjectOfType(
                DynValue.NewNumber((double)SampleEnum.Second),
                typeof(SampleEnum),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.TypeOf<int>());
            Assert.That(result, Is.EqualTo((int)SampleEnum.Second));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsFunctionToClosure()
        {
            DynValue functionValue = CreateConstantClosure("return 1337");

            object result = ScriptToClrConversions.DynValueToObjectOfType(
                functionValue,
                typeof(Closure),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.SameAs(functionValue.Function));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsFunctionToScriptFunctionDelegate()
        {
            DynValue functionValue = CreateConstantClosure("return 21 + 21");

            object result = ScriptToClrConversions.DynValueToObjectOfType(
                functionValue,
                typeof(ScriptFunctionDelegate),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.InstanceOf<ScriptFunctionDelegate>());
            ScriptFunctionDelegate functionDelegate = (ScriptFunctionDelegate)result;
            object invocationResult = functionDelegate(Array.Empty<object>());
            Assert.That(invocationResult, Is.EqualTo(42d));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsTableToGenericList()
        {
            Table table = new(null);
            table.Append(DynValue.NewNumber(10));
            table.Append(DynValue.NewNumber(20));
            DynValue dynValueTable = DynValue.NewTable(table);

            object result = ScriptToClrConversions.DynValueToObjectOfType(
                dynValueTable,
                typeof(List<int>),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.InstanceOf<List<int>>());
            Assert.That(result, Is.EqualTo(new List<int> { 10, 20 }));
        }

        [Test]
        public void DynValueToObjectOfTypeReturnsDescriptorObjectWhenCompatible()
        {
            FakeUserData instance = new();
            IUserDataDescriptor descriptor = new TestUserDataDescriptor(
                typeof(FakeUserData),
                isTypeCompatible: true,
                stringValue: null
            );
            DynValue userData = UserData.Create(instance, descriptor);

            object result = ScriptToClrConversions.DynValueToObjectOfType(
                userData,
                typeof(FakeUserData),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.SameAs(instance));
        }

        [Test]
        public void DynValueToObjectOfTypeFallsBackToDescriptorString()
        {
            FakeUserData instance = new();
            IUserDataDescriptor descriptor = new TestUserDataDescriptor(
                typeof(FakeUserData),
                isTypeCompatible: false,
                stringValue: "<userdata>"
            );
            DynValue userData = UserData.Create(instance, descriptor);

            object result = ScriptToClrConversions.DynValueToObjectOfType(
                userData,
                typeof(string),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.EqualTo("<userdata>"));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsClrFunctionToCallbackFunction()
        {
            DynValue callbackDynValue = DynValue.NewCallback(
                (ctx, args) => DynValue.NewString("ok"),
                "cb"
            );

            object result = ScriptToClrConversions.DynValueToObjectOfType(
                callbackDynValue,
                typeof(CallbackFunction),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.SameAs(callbackDynValue.Callback));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsCallbackFunctionToDelegate()
        {
            DynValue callbackDynValue = DynValue.NewCallback(
                (ctx, args) => DynValue.NewNumber(42),
                "answer"
            );

            object result = ScriptToClrConversions.DynValueToObjectOfType(
                callbackDynValue,
                typeof(Func<ScriptExecutionContext, CallbackArguments, DynValue>),
                defaultValue: null,
                isOptional: false
            );

            Func<ScriptExecutionContext, CallbackArguments, DynValue> delegateResult =
                (Func<ScriptExecutionContext, CallbackArguments, DynValue>)result;
            DynValue invocationResult = delegateResult(
                null,
                new CallbackArguments(Array.Empty<DynValue>(), isMethodCall: false)
            );

            Assert.That(invocationResult.Number, Is.EqualTo(42));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsTableToDictionary()
        {
            Script script = new Script();
            Table table = new Table(script);
            table.Set(DynValue.NewString("alpha"), DynValue.NewNumber(1));
            table.Set(DynValue.NewNumber(2), DynValue.NewString("beta"));
            DynValue tableValue = DynValue.NewTable(table);

            object result = ScriptToClrConversions.DynValueToObjectOfType(
                tableValue,
                typeof(Dictionary<object, object>),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.InstanceOf<Dictionary<object, object>>());
            Dictionary<object, object> dictionary = (Dictionary<object, object>)result;
            Assert.That(dictionary["alpha"], Is.EqualTo(1.0));
            Assert.That(dictionary[2.0], Is.EqualTo("beta"));
        }

        [Test]
        public void DynValueToObjectOfTypeConvertsTableToGenericDictionary()
        {
            Script script = new Script();
            Table table = new Table(script);
            table.Set(DynValue.NewString("one"), DynValue.NewNumber(1));
            table.Set(DynValue.NewString("two"), DynValue.NewNumber(2));
            DynValue tableValue = DynValue.NewTable(table);

            object result = ScriptToClrConversions.DynValueToObjectOfType(
                tableValue,
                typeof(Dictionary<string, int>),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.InstanceOf<Dictionary<string, int>>());
            Dictionary<string, int> dictionary = (Dictionary<string, int>)result;
            Assert.That(dictionary["one"], Is.EqualTo(1));
            Assert.That(dictionary["two"], Is.EqualTo(2));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsTableToDictionaryWeight()
        {
            Table table = new Table(new Script());
            table.Set(DynValue.NewString("key"), DynValue.NewNumber(42));
            DynValue tableValue = DynValue.NewTable(table);

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                tableValue,
                typeof(Dictionary<string, double>),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_TABLE_CONVERSION));
        }

        [Test]
        public void DynValueToObjectOfTypeThrowsForTupleConversions()
        {
            DynValue tupleValue = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(
                () =>
                    ScriptToClrConversions.DynValueToObjectOfType(
                        tupleValue,
                        typeof(int),
                        defaultValue: null,
                        isOptional: false
                    )
            );

            Assert.That(exception.Message, Does.Contain("convert"));
        }

        [Test]
        public void DynValueToObjectOfTypeThrowsForEmptyStringCharConversions()
        {
            DynValue empty = DynValue.NewString(string.Empty);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(
                () =>
                    ScriptToClrConversions.DynValueToObjectOfType(
                        empty,
                        typeof(char),
                        defaultValue: null,
                        isOptional: false
                    )
            );

            Assert.That(exception.Message, Does.Contain("convert"));
        }

        [Test]
        public void DynValueToObjectOfTypeHonorsCustomConverters()
        {
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(
                DataType.Number,
                typeof(int),
                dv => 999
            );

            object result = ScriptToClrConversions.DynValueToObjectOfType(
                DynValue.NewNumber(1.23),
                typeof(int),
                defaultValue: null,
                isOptional: false
            );

            Assert.That(result, Is.EqualTo(999));
        }

        [Test]
        public void DynValueToObjectOfTypeThrowsWhenNoConversionExists()
        {
            Assert.Throws<ScriptRuntimeException>(
                () =>
                    ScriptToClrConversions.DynValueToObjectOfType(
                        DynValue.NewBoolean(true),
                        typeof(DateTime),
                        defaultValue: null,
                        isOptional: false
                    )
            );
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsNilToNullableWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.Nil,
                typeof(int?),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_NIL_TO_NULLABLE));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsNumberDowncastWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.NewNumber(3.14),
                typeof(int),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_NUMBER_DOWNCAST));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsStringToCharWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.NewString("nova"),
                typeof(char),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_STRING_TO_CHAR));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsTableConversionWeight()
        {
            Table table = new(null);
            table.Append(DynValue.NewNumber(1));
            DynValue tableValue = DynValue.NewTable(table);

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                tableValue,
                typeof(List<int>),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_TABLE_CONVERSION));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsCustomConverterMatch()
        {
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(
                DataType.Boolean,
                typeof(string),
                dv => dv.Boolean ? "yes" : "no"
            );

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.NewBoolean(true),
                typeof(string),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_CUSTOM_CONVERTER_MATCH));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsExactMatchForDynValueRequests()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.NewNumber(0),
                typeof(DynValue),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_EXACT_MATCH));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsExactMatchForClosureConversions()
        {
            DynValue functionValue = CreateConstantClosure("return 1");

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                functionValue,
                typeof(Closure),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_EXACT_MATCH));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsExactMatchForScriptFunctionDelegates()
        {
            DynValue functionValue = CreateConstantClosure("return 2");

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                functionValue,
                typeof(ScriptFunctionDelegate),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_EXACT_MATCH));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsExactMatchForCallbackFunctions()
        {
            DynValue callbackValue = DynValue.NewCallback((ctx, args) => DynValue.NewNumber(3));

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                callbackValue,
                typeof(CallbackFunction),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_EXACT_MATCH));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsExactMatchForCallbackDelegates()
        {
            DynValue callbackValue = DynValue.NewCallback((ctx, args) => DynValue.NewNumber(4));

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                callbackValue,
                typeof(Func<ScriptExecutionContext, CallbackArguments, DynValue>),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_EXACT_MATCH));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsNilWithDefaultWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.Nil,
                typeof(int),
                isOptional: true
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_NIL_WITH_DEFAULT));
        }

        [Test]
        public void DynValueToObjectOfTypeWeightReturnsExactMatchForTableType()
        {
            DynValue table = DynValue.NewTable(new Table(null));

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                table,
                typeof(Table),
                isOptional: false
            );

            Assert.That(weight, Is.EqualTo(ScriptToClrConversions.WEIGHT_EXACT_MATCH));
        }

        private static DynValue CreateConstantClosure(string code)
        {
            Script script = new();
            return script.LoadString(code);
        }

        private enum SampleEnum
        {
            First = 1,
            Second = 2,
        }

        private sealed class FakeUserData { }

        private sealed class TestUserDataDescriptor : IUserDataDescriptor
        {
            private readonly bool _isTypeCompatible;
            private readonly string _stringValue;
            private readonly Type _type;

            internal TestUserDataDescriptor(Type type, bool isTypeCompatible, string stringValue)
            {
                _type = type ?? typeof(object);
                _isTypeCompatible = isTypeCompatible;
                _stringValue = stringValue ?? "<user>";
            }

            public string Name => "TestDescriptor";

            public Type Type => _type;

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
            {
                return DynValue.Nil;
            }

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
                bool isDirectIndexing
            )
            {
                return false;
            }

            public string AsString(object obj)
            {
                return _stringValue ?? obj?.ToString();
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                return null;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                if (!_isTypeCompatible || obj == null)
                {
                    return false;
                }

                return type.IsInstanceOfType(obj);
            }
        }
    }
}
