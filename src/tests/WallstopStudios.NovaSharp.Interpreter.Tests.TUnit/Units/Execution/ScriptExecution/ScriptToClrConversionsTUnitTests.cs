namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Converters;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [ScriptGlobalOptionsIsolation]
    public sealed class ScriptToClrConversionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DynValueToObjectUsesCustomConversionResult()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                    registry.SetScriptToClrCustomConversion(
                        DataType.String,
                        typeof(object),
                        dv => $"converted:{dv.String}"
                    )
            );

            object result = ScriptToClrConversions.DynValueToObject(DynValue.NewString("lua"));

            await Assert.That(result).IsEqualTo("converted:lua");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsNullForVoidValues()
        {
            object result = ScriptToClrConversions.DynValueToObject(DynValue.Void);
            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsClosureReference()
        {
            DynValue closure = CreateConstantClosure("return 41 + 1");

            object result = ScriptToClrConversions.DynValueToObject(closure);

            await Assert.That(result is Closure).IsTrue();
            await Assert.That(ReferenceEquals(result, closure.Function)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsTableReference()
        {
            Table table = new(null);
            table.Set("language", DynValue.NewString("Lua"));
            DynValue tableValue = DynValue.NewTable(table);

            object result = ScriptToClrConversions.DynValueToObject(tableValue);

            await Assert.That(ReferenceEquals(result, table)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsTupleArray()
        {
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewString("two");
            DynValue tupleValue = DynValue.NewTuple(first, second);

            object result = ScriptToClrConversions.DynValueToObject(tupleValue);

            await Assert.That(result is DynValue[]).IsTrue();
            DynValue[] tuple = (DynValue[])result;
            await Assert.That(tuple.Length).IsEqualTo(2);
            await Assert.That(tuple[0]).IsEqualTo(first);
            await Assert.That(tuple[1]).IsEqualTo(second);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsDescriptorTypeWhenNoInstanceIsAvailable()
        {
            IUserDataDescriptor descriptor = new TestUserDataDescriptor(
                typeof(FakeUserData),
                isTypeCompatible: false,
                stringValue: "<unused>"
            );
            DynValue userData = UserData.CreateStatic(descriptor);

            object result = ScriptToClrConversions.DynValueToObject(userData);

            await Assert.That(result).IsEqualTo(descriptor.Type);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsCallbackFunctionInstance()
        {
            DynValue callback = DynValue.NewCallback((ctx, args) => DynValue.NewNumber(5));

            object result = ScriptToClrConversions.DynValueToObject(callback);

            await Assert.That(result is CallbackFunction).IsTrue();
            await Assert.That(ReferenceEquals(result, callback.Callback)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectThrowsForUnsupportedTypes()
        {
            DynValue yieldRequest = DynValue.NewYieldReq(Array.Empty<DynValue>());

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                ScriptToClrConversions.DynValueToObject(yieldRequest)
            );

            await Assert.That(exception.Message.Length > 0).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeReturnsDefaultForOptionalVoid()
        {
            int result = ScriptToClrConversions.DynValueToObjectOfType<int>(
                DynValue.Void,
                defaultValue: 77,
                isOptional: true
            );

            await Assert.That(result).IsEqualTo(77);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsNilToNullable()
        {
            int? result = ScriptToClrConversions.DynValueToObjectOfType<int?>(
                DynValue.Nil,
                defaultValue: null,
                isOptional: false
            );

            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeReturnsDefaultForOptionalNilValueType()
        {
            int result = ScriptToClrConversions.DynValueToObjectOfType<int>(
                DynValue.Nil,
                defaultValue: 123,
                isOptional: true
            );

            await Assert.That(result).IsEqualTo(123);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsBooleanToStringBuilder()
        {
            StringBuilder result = ScriptToClrConversions.DynValueToObjectOfType<StringBuilder>(
                DynValue.NewBoolean(true),
                defaultValue: null,
                isOptional: false
            );

            await Assert.That(result.ToString()).IsEqualTo("True");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsStringToChar()
        {
            char result = ScriptToClrConversions.DynValueToObjectOfType<char>(
                DynValue.NewString("Nova"),
                defaultValue: default(char),
                isOptional: false
            );

            await Assert.That(result).IsEqualTo('N');
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsNumberToEnum()
        {
            SampleEnum result = ScriptToClrConversions.DynValueToObjectOfType<SampleEnum>(
                DynValue.NewNumber((double)SampleEnum.Second),
                defaultValue: SampleEnum.First,
                isOptional: false
            );

            await Assert.That(result).IsEqualTo(SampleEnum.Second);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeGenericReturnsTypedValue()
        {
            int result = ScriptToClrConversions.DynValueToObjectOfType<int>(
                DynValue.NewNumber(42),
                isOptional: false
            );

            await Assert.That(result).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeGenericHonorsDefaultValue()
        {
            int result = ScriptToClrConversions.DynValueToObjectOfType<int>(
                DynValue.Void,
                defaultValue: 77,
                isOptional: true
            );

            await Assert.That(result).IsEqualTo(77);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsFunctionToClosure()
        {
            DynValue functionValue = CreateConstantClosure("return 1337");

            Closure result = ScriptToClrConversions.DynValueToObjectOfType<Closure>(
                functionValue,
                defaultValue: null,
                isOptional: false
            );

            await Assert.That(ReferenceEquals(result, functionValue.Function)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsFunctionToScriptFunctionCallback()
        {
            DynValue functionValue = CreateConstantClosure("return 21 + 21");

            ScriptFunctionCallback result =
                ScriptToClrConversions.DynValueToObjectOfType<ScriptFunctionCallback>(
                    functionValue,
                    defaultValue: null,
                    isOptional: false
                );

            object invocationResult = result(Array.Empty<object>());
            await Assert.That(invocationResult).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsTableToGenericList()
        {
            Table table = new(null);
            table.Append(DynValue.NewNumber(10));
            table.Append(DynValue.NewNumber(20));
            DynValue dynValueTable = DynValue.NewTable(table);

            List<int> result = ScriptToClrConversions.DynValueToObjectOfType<List<int>>(
                dynValueTable,
                defaultValue: null,
                isOptional: false
            );

            await Assert.That(result.Count).IsEqualTo(2);
            await Assert.That(result[0]).IsEqualTo(10);
            await Assert.That(result[1]).IsEqualTo(20);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeReturnsDescriptorObjectWhenCompatible()
        {
            FakeUserData instance = new();
            IUserDataDescriptor descriptor = new TestUserDataDescriptor(
                typeof(FakeUserData),
                isTypeCompatible: true,
                stringValue: null
            );
            DynValue userData = UserData.Create(instance, descriptor);

            FakeUserData result = ScriptToClrConversions.DynValueToObjectOfType<FakeUserData>(
                userData,
                defaultValue: null,
                isOptional: false
            );

            await Assert.That(ReferenceEquals(result, instance)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeFallsBackToDescriptorString()
        {
            FakeUserData instance = new();
            IUserDataDescriptor descriptor = new TestUserDataDescriptor(
                typeof(FakeUserData),
                isTypeCompatible: false,
                stringValue: "<userdata>"
            );
            DynValue userData = UserData.Create(instance, descriptor);

            string result = ScriptToClrConversions.DynValueToObjectOfType<string>(
                userData,
                defaultValue: null,
                isOptional: false
            );

            await Assert.That(result).IsEqualTo("<userdata>");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsClrFunctionToCallbackFunction()
        {
            DynValue callbackDynValue = DynValue.NewCallback(
                (ctx, args) => DynValue.NewString("ok"),
                "cb"
            );

            CallbackFunction result =
                ScriptToClrConversions.DynValueToObjectOfType<CallbackFunction>(
                    callbackDynValue,
                    defaultValue: null,
                    isOptional: false
                );

            await Assert.That(ReferenceEquals(result, callbackDynValue.Callback)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsCallbackFunctionToDelegate()
        {
            DynValue callbackDynValue = DynValue.NewCallback(
                (ctx, args) => DynValue.NewNumber(42),
                "answer"
            );

            Func<ScriptExecutionContext, CallbackArguments, DynValue> delegateResult =
                ScriptToClrConversions.DynValueToObjectOfType<
                    Func<ScriptExecutionContext, CallbackArguments, DynValue>
                >(callbackDynValue, defaultValue: null, isOptional: false);
            DynValue invocationResult = delegateResult(
                null,
                new CallbackArguments(Array.Empty<DynValue>(), isMethodCall: false)
            );

            await Assert.That(invocationResult.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsTableToDictionary()
        {
            Script script = new();
            Table table = new(script);
            table.Set(DynValue.NewString("alpha"), DynValue.NewNumber(1));
            table.Set(DynValue.NewNumber(2), DynValue.NewString("beta"));
            DynValue tableValue = DynValue.NewTable(table);

            Dictionary<object, object> result = ScriptToClrConversions.DynValueToObjectOfType<
                Dictionary<object, object>
            >(tableValue, defaultValue: null, isOptional: false);

            await Assert.That(result["alpha"]).IsEqualTo(1.0);
            await Assert.That(result[2.0]).IsEqualTo("beta");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsTableToGenericDictionary()
        {
            Script script = new();
            Table table = new(script);
            table.Set(DynValue.NewString("one"), DynValue.NewNumber(1));
            table.Set(DynValue.NewString("two"), DynValue.NewNumber(2));
            DynValue tableValue = DynValue.NewTable(table);

            Dictionary<string, int> result = ScriptToClrConversions.DynValueToObjectOfType<
                Dictionary<string, int>
            >(tableValue, defaultValue: null, isOptional: false);

            await Assert.That(result["one"]).IsEqualTo(1);
            await Assert.That(result["two"]).IsEqualTo(2);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsTableToDictionaryWeight()
        {
            Table table = new(new Script());
            table.Set(DynValue.NewString("key"), DynValue.NewNumber(42));
            DynValue tableValue = DynValue.NewTable(table);

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                tableValue,
                typeof(Dictionary<string, double>),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightTableConversion);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeThrowsForTupleConversions()
        {
            DynValue tupleValue = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2));

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                ScriptToClrConversions.DynValueToObjectOfType<int>(
                    tupleValue,
                    defaultValue: 0,
                    isOptional: false
                )
            );

            await Assert
                .That(exception.Message.Contains("convert", StringComparison.OrdinalIgnoreCase))
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeThrowsForEmptyStringCharConversions()
        {
            DynValue empty = DynValue.NewString(string.Empty);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                ScriptToClrConversions.DynValueToObjectOfType<char>(
                    empty,
                    defaultValue: default(char),
                    isOptional: false
                )
            );

            await Assert
                .That(exception.Message.Contains("convert", StringComparison.OrdinalIgnoreCase))
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeHonorsCustomConverters()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                    registry.SetScriptToClrCustomConversion(DataType.Number, typeof(int), dv => 999)
            );

            int result = ScriptToClrConversions.DynValueToObjectOfType<int>(
                DynValue.NewNumber(1.23),
                defaultValue: 0,
                isOptional: false
            );

            await Assert.That(result).IsEqualTo(999);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeThrowsWhenNoConversionExists()
        {
            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                ScriptToClrConversions.DynValueToObjectOfType<DateTime>(
                    DynValue.NewBoolean(true),
                    defaultValue: default(DateTime),
                    isOptional: false
                )
            );

            await Assert.That(exception.Message.Length > 0).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsNilToNullableWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.Nil,
                typeof(int?),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightNilToNullable);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsNumberDowncastWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.NewNumber(3.14),
                typeof(int),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightNumberDowncast);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsStringToCharWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.NewString("nova"),
                typeof(char),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightStringToChar);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsTableConversionWeight()
        {
            Table table = new(null);
            table.Append(DynValue.NewNumber(1));
            DynValue tableValue = DynValue.NewTable(table);

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                tableValue,
                typeof(List<int>),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightTableConversion);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsCustomConverterMatch()
        {
            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                    registry.SetScriptToClrCustomConversion(
                        DataType.Boolean,
                        typeof(string),
                        dv => dv.Boolean ? "yes" : "no"
                    )
            );

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.NewBoolean(true),
                typeof(string),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightCustomConverterMatch);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsExactMatchForDynValueRequests()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.NewNumber(0),
                typeof(DynValue),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightExactMatch);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsExactMatchForClosureConversions()
        {
            DynValue functionValue = CreateConstantClosure("return 1");

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                functionValue,
                typeof(Closure),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightExactMatch);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsExactMatchForScriptFunctionCallbacks()
        {
            DynValue functionValue = CreateConstantClosure("return 2");

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                functionValue,
                typeof(ScriptFunctionCallback),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightExactMatch);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsExactMatchForCallbackFunctions()
        {
            DynValue callbackValue = DynValue.NewCallback((ctx, args) => DynValue.NewNumber(3));

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                callbackValue,
                typeof(CallbackFunction),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightExactMatch);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsExactMatchForCallbackDelegates()
        {
            DynValue callbackValue = DynValue.NewCallback((ctx, args) => DynValue.NewNumber(4));

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                callbackValue,
                typeof(Func<ScriptExecutionContext, CallbackArguments, DynValue>),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightExactMatch);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsNilWithDefaultWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                DynValue.Nil,
                typeof(int),
                isOptional: true
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightNilWithDefault);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsExactMatchForTableType()
        {
            DynValue table = DynValue.NewTable(new Table(null));

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                table,
                typeof(Table),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightExactMatch);
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

            public DynValue Index(
                Script script,
                object obj,
                DynValue index,
                bool isDirectIndexing
            ) => DynValue.Nil;

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

            public DynValue MetaIndex(Script script, object obj, string metaname) => null;

            public bool IsTypeCompatible(Type type, object obj)
            {
                if (!_isTypeCompatible || obj == null)
                {
                    return false;
                }

                return type.IsInstanceOfType(obj);
            }
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
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
