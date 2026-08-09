namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Converters;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

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

            object result = ScriptToClrConversions.DynValueToObject(LuaValue.NewString("lua"));

            await Assert.That(result).IsEqualTo("converted:lua");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectPreservesIntegerSubtype()
        {
            // When LuaValue is an integer subtype, ToObject should return long (not double)
            LuaValue intValue = LuaValue.NewInteger(9007199254740993L); // Beyond double precision

            object result = ScriptToClrConversions.DynValueToObject(intValue);

            await Assert.That(result is long).IsTrue().ConfigureAwait(false);
            await Assert.That((long)result).IsEqualTo(9007199254740993L).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsDoubleForFloatSubtype()
        {
            // When LuaValue is a float subtype, ToObject should return double
            LuaValue floatValue = LuaValue.NewFloat(3.14159);

            object result = ScriptToClrConversions.DynValueToObject(floatValue);

            await Assert.That(result is double).IsTrue().ConfigureAwait(false);
            await Assert.That((double)result).IsEqualTo(3.14159).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeLongPreservesIntegerPrecision()
        {
            // Large integer beyond double precision should be preserved when converting to long
            LuaValue intValue = LuaValue.NewInteger(9007199254740993L);

            long result = ScriptToClrConversions.DynValueToObjectOfType<long>(intValue);

            await Assert.That(result).IsEqualTo(9007199254740993L).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsNullForVoidValues()
        {
            object result = ScriptToClrConversions.DynValueToObject(LuaValue.Void);
            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsClosureReference()
        {
            LuaValue closure = CreateConstantClosure("return 41 + 1");

            object result = ScriptToClrConversions.DynValueToObject(closure);

            await Assert.That(result is Closure).IsTrue();
            await Assert.That(ReferenceEquals(result, closure.Function)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsTableReference()
        {
            Table table = new(null);
            table.Set("language", LuaValue.NewString("Lua"));
            LuaValue tableValue = LuaValue.NewTable(table);

            object result = ScriptToClrConversions.DynValueToObject(tableValue);

            await Assert.That(ReferenceEquals(result, table)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsTupleArray()
        {
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewString("two");
            LuaValue tupleValue = LuaValue.NewTuple(first, second);

            object result = ScriptToClrConversions.DynValueToObject(tupleValue);

            await Assert.That(result is LuaValue[]).IsTrue();
            LuaValue[] tuple = (LuaValue[])result;
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
            LuaValue? userData = UserData.CreateStatic(descriptor);

            await Assert.That(userData.HasValue).IsTrue();
            object result = ScriptToClrConversions.DynValueToObject(userData.Value);

            await Assert.That(result).IsEqualTo(descriptor.Type);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsCallbackFunctionInstance()
        {
            LuaValue callback = LuaValue.NewCallback((ctx, args) => LuaValue.NewNumber(5));

            object result = ScriptToClrConversions.DynValueToObject(callback);

            await Assert.That(result is CallbackFunction).IsTrue();
            await Assert.That(ReferenceEquals(result, callback.Callback)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectThrowsForUnsupportedTypes()
        {
            LuaValue yieldRequest = LuaValue.NewYieldReq(Array.Empty<LuaValue>());

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                ScriptToClrConversions.DynValueToObject(yieldRequest)
            );

            await Assert.That(exception.Message.Length > 0).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeReturnsDefaultForOptionalVoid()
        {
            int result = ScriptToClrConversions.DynValueToObjectOfType<int>(
                LuaValue.Void,
                defaultValue: 77,
                isOptional: true
            );

            await Assert.That(result).IsEqualTo(77);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsNilToNullable()
        {
            int? result = ScriptToClrConversions.DynValueToObjectOfType<int?>(
                LuaValue.Nil,
                defaultValue: null,
                isOptional: false
            );

            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeReturnsDefaultForOptionalNilValueType()
        {
            int result = ScriptToClrConversions.DynValueToObjectOfType<int>(
                LuaValue.Nil,
                defaultValue: 123,
                isOptional: true
            );

            await Assert.That(result).IsEqualTo(123);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsBooleanToStringBuilder()
        {
            StringBuilder result = ScriptToClrConversions.DynValueToObjectOfType<StringBuilder>(
                LuaValue.NewBoolean(true),
                defaultValue: null,
                isOptional: false
            );

            await Assert.That(result.ToString()).IsEqualTo("True");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsStringToChar()
        {
            char result = ScriptToClrConversions.DynValueToObjectOfType<char>(
                LuaValue.NewString("Nova"),
                defaultValue: default(char),
                isOptional: false
            );

            await Assert.That(result).IsEqualTo('N');
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsNumberToEnum()
        {
            SampleEnum result = ScriptToClrConversions.DynValueToObjectOfType<SampleEnum>(
                LuaValue.NewNumber((double)SampleEnum.Second),
                defaultValue: SampleEnum.First,
                isOptional: false
            );

            await Assert.That(result).IsEqualTo(SampleEnum.Second);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeGenericReturnsTypedValue()
        {
            int result = ScriptToClrConversions.DynValueToObjectOfType<int>(
                LuaValue.NewNumber(42),
                isOptional: false
            );

            await Assert.That(result).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeGenericHonorsDefaultValue()
        {
            int result = ScriptToClrConversions.DynValueToObjectOfType<int>(
                LuaValue.Void,
                defaultValue: 77,
                isOptional: true
            );

            await Assert.That(result).IsEqualTo(77);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsFunctionToClosure()
        {
            LuaValue functionValue = CreateConstantClosure("return 1337");

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
            LuaValue functionValue = CreateConstantClosure("return 21 + 21");

            ScriptFunctionCallback result =
                ScriptToClrConversions.DynValueToObjectOfType<ScriptFunctionCallback>(
                    functionValue,
                    defaultValue: null,
                    isOptional: false
                );

            object invocationResult = result(Array.Empty<object>());
            // Result may be long or double depending on internal Lua number representation
            await Assert
                .That(Convert.ToDouble(invocationResult, CultureInfo.InvariantCulture))
                .IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsTableToGenericList()
        {
            Table table = new(null);
            table.Append(LuaValue.NewNumber(10));
            table.Append(LuaValue.NewNumber(20));
            LuaValue dynValueTable = LuaValue.NewTable(table);

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
            LuaValue userData = UserData.Create(instance, descriptor);

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
            LuaValue userData = UserData.Create(instance, descriptor);

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
            LuaValue callbackDynValue = LuaValue.NewCallback(
                (ctx, args) => LuaValue.NewString("ok"),
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
            LuaValue callbackDynValue = LuaValue.NewCallback(
                (ctx, args) => LuaValue.NewNumber(42),
                "answer"
            );

            Func<ScriptExecutionContext, CallbackArguments, LuaValue> delegateResult =
                ScriptToClrConversions.DynValueToObjectOfType<
                    Func<ScriptExecutionContext, CallbackArguments, LuaValue>
                >(callbackDynValue, defaultValue: null, isOptional: false);
            LuaValue invocationResult = delegateResult(
                null,
                new CallbackArguments(Array.Empty<LuaValue>(), isMethodCall: false)
            );

            await Assert.That(invocationResult.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsTableToDictionary()
        {
            Script script = new();
            Table table = new(script);
            table.SetValue(LuaValue.NewString("alpha"), LuaValue.NewNumber(1));
            table.SetValue(LuaValue.NewNumber(2), LuaValue.NewString("beta"));
            LuaValue tableValue = LuaValue.NewTable(table);

            Dictionary<object, object> result = ScriptToClrConversions.DynValueToObjectOfType<
                Dictionary<object, object>
            >(tableValue, defaultValue: null, isOptional: false);

            // Numeric values may be long or double depending on internal representation
            await Assert
                .That(Convert.ToDouble(result["alpha"], CultureInfo.InvariantCulture))
                .IsEqualTo(1.0);
            // Numeric key may be stored as long, so try both
            object betaValue = result.TryGetValue(2L, out object longKeyValue)
                ? longKeyValue
                : result[2.0];
            await Assert.That(betaValue).IsEqualTo("beta");
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeConvertsTableToGenericDictionary()
        {
            Script script = new();
            Table table = new(script);
            table.SetValue(LuaValue.NewString("one"), LuaValue.NewNumber(1));
            table.SetValue(LuaValue.NewString("two"), LuaValue.NewNumber(2));
            LuaValue tableValue = LuaValue.NewTable(table);

            Dictionary<string, int> result = ScriptToClrConversions.DynValueToObjectOfType<
                Dictionary<string, int>
            >(tableValue, defaultValue: null, isOptional: false);

            await Assert.That(result["one"]).IsEqualTo(1);
            await Assert.That(result["two"]).IsEqualTo(2);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynValueToObjectOfTypeWeightReturnsTableToDictionaryWeight(
            LuaCompatibilityVersion version
        )
        {
            Table table = new(new Script(version));
            table.SetValue(LuaValue.NewString("key"), LuaValue.NewNumber(42));
            LuaValue tableValue = LuaValue.NewTable(table);

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
            LuaValue tupleValue = LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewNumber(2));

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
            LuaValue empty = LuaValue.NewString(string.Empty);

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
                LuaValue.NewNumber(1.23),
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
                    LuaValue.NewBoolean(true),
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
                LuaValue.Nil,
                typeof(int?),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightNilToNullable);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsNumberDowncastWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                LuaValue.NewNumber(3.14),
                typeof(int),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightNumberDowncast);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsStringToCharWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                LuaValue.NewString("nova"),
                typeof(char),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightStringToChar);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsTableConversionWeight()
        {
            Table table = new(null);
            table.Append(LuaValue.NewNumber(1));
            LuaValue tableValue = LuaValue.NewTable(table);

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
                LuaValue.NewBoolean(true),
                typeof(string),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightCustomConverterMatch);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsExactMatchForDynValueRequests()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                LuaValue.NewNumber(0),
                typeof(LuaValue),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightExactMatch);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsExactMatchForClosureConversions()
        {
            LuaValue functionValue = CreateConstantClosure("return 1");

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
            LuaValue functionValue = CreateConstantClosure("return 2");

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
            LuaValue callbackValue = LuaValue.NewCallback((ctx, args) => LuaValue.NewNumber(3));

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
            LuaValue callbackValue = LuaValue.NewCallback((ctx, args) => LuaValue.NewNumber(4));

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                callbackValue,
                typeof(Func<ScriptExecutionContext, CallbackArguments, LuaValue>),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightExactMatch);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsNilWithDefaultWeight()
        {
            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                LuaValue.Nil,
                typeof(int),
                isOptional: true
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightNilWithDefault);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectOfTypeWeightReturnsExactMatchForTableType()
        {
            LuaValue table = LuaValue.NewTable(new Table(null));

            int weight = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                table,
                typeof(Table),
                isOptional: false
            );

            await Assert.That(weight).IsEqualTo(ScriptToClrConversions.WeightExactMatch);
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsLongForIntegerValues()
        {
            // Lua 5.3+ integer subtype compliance: integer values should convert to long
            LuaValue integerValue = LuaValue.NewNumber(42);

            object result = ScriptToClrConversions.DynValueToObject(integerValue);

            // Value should be numerically correct
            await Assert
                .That(Convert.ToDouble(result, CultureInfo.InvariantCulture))
                .IsEqualTo(42d);
            // For integer literals, result should be long (not double)
            await Assert.That(result is long).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DynValueToObjectReturnsDoubleForFloatValues()
        {
            // Float values should convert to double
            LuaValue floatValue = LuaValue.NewNumber(3.14);

            object result = ScriptToClrConversions.DynValueToObject(floatValue);

            await Assert.That(result).IsEqualTo(3.14);
            await Assert.That(result is double).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(0)]
        [global::TUnit.Core.Arguments(-1)]
        [global::TUnit.Core.Arguments(100)]
        [global::TUnit.Core.Arguments(int.MaxValue)]
        [global::TUnit.Core.Arguments(int.MinValue)]
        public async Task DynValueToObjectReturnsLongForVariousIntegerLiterals(int expectedValue)
        {
            LuaValue integerValue = LuaValue.NewNumber(expectedValue);

            object result = ScriptToClrConversions.DynValueToObject(integerValue);

            await Assert
                .That(Convert.ToInt64(result, CultureInfo.InvariantCulture))
                .IsEqualTo((long)expectedValue);
            await Assert.That(result is long).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0.0)]
        [global::TUnit.Core.Arguments(1.5)]
        [global::TUnit.Core.Arguments(-3.14159)]
        [global::TUnit.Core.Arguments(double.MaxValue)]
        [global::TUnit.Core.Arguments(double.MinValue)]
        [global::TUnit.Core.Arguments(double.Epsilon)]
        public async Task DynValueToObjectReturnsDoubleForVariousFloatLiterals(double expectedValue)
        {
            LuaValue floatValue = LuaValue.NewNumber(expectedValue);

            object result = ScriptToClrConversions.DynValueToObject(floatValue);

            await Assert
                .That(Convert.ToDouble(result, CultureInfo.InvariantCulture))
                .IsEqualTo(expectedValue);
            // Non-integer values should remain as double
            if (expectedValue != Math.Floor(expectedValue))
            {
                await Assert.That(result is double).IsTrue();
            }
        }

        private static LuaValue CreateConstantClosure(string code)
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

            public bool TryIndex(
                Script script,
                object obj,
                LuaValue index,
                bool isDirectIndexing,
                out LuaValue value
            )
            {
                value = LuaValue.Nil;
                return true;
            }

            public bool SetIndex(
                Script script,
                object obj,
                LuaValue index,
                LuaValue value,
                bool isDirectIndexing
            )
            {
                return false;
            }

            public string AsString(object obj)
            {
                return _stringValue ?? obj?.ToString();
            }

            public bool TryMetaIndex(Script script, object obj, string metaname, out LuaValue value)
            {
                value = LuaValue.Nil;
                return false;
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
