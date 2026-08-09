namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [UserDataIsolation]
    public sealed class DynValueTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task NewTupleHandlesEmptyAndSingleInputs()
        {
            LuaValue empty = LuaValue.NewTuple();
            LuaValue single = LuaValue.NewNumber(42);
            LuaValue wrappedSingle = LuaValue.NewTuple(single);

            await Assert.That(empty).IsEqualTo(LuaValue.EmptyTuple).ConfigureAwait(false);

            await Assert.That(wrappedSingle).IsEqualTo(single).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleTreatsSingleNullInputAsNil()
        {
            LuaValue singleOverload = LuaValue.NewTuple(default(LuaValue));
            LuaValue paramsOverload = LuaValue.NewTuple(new LuaValue[] { default });

            await Assert.That(singleOverload.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(paramsOverload.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(2, false, 1)]
        [global::TUnit.Core.Arguments(3, false, 1)]
        [global::TUnit.Core.Arguments(4, false, 2)]
        [global::TUnit.Core.Arguments(5, true, 2)]
        public async Task NewTupleTreatsMultiValueNullInputsAsNil(
            int arity,
            bool useParamsArray,
            int expectedNilCount
        )
        {
            LuaValue one = LuaValue.NewNumber(1);
            LuaValue two = LuaValue.NewNumber(2);
            LuaValue tuple = (arity, useParamsArray) switch
            {
                (2, false) => LuaValue.NewTuple(default, one),
                (3, false) => LuaValue.NewTuple(one, default, two),
                (4, false) => LuaValue.NewTuple(default, one, default, two),
                (5, true) => LuaValue.NewTuple(
                    new LuaValue[] { one, default, two, default, LuaValue.NewBoolean(true) }
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(arity), arity, null),
            };

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(tuple.Tuple.Length).IsEqualTo(arity).ConfigureAwait(false);

            int nilCount = 0;
            foreach (LuaValue value in tuple.Tuple)
            {
                if (value.Type == DataType.Nil)
                {
                    ++nilCount;
                }
            }

            await Assert.That(nilCount).IsEqualTo(expectedNilCount).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleNestedFlattensTuplesOneLevelDeep()
        {
            LuaValue tupleA = LuaValue.NewTuple(LuaValue.NewString("a"), LuaValue.NewString("b"));
            LuaValue tupleB = LuaValue.NewTuple(LuaValue.NewNumber(3), LuaValue.NewNumber(4));

            LuaValue flattened = LuaValue.NewTupleNested(
                tupleA,
                tupleB,
                LuaValue.NewString("tail")
            );

            await Assert.That(flattened.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);

            await Assert.That(flattened.Tuple.Length).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(flattened.Tuple[0].String).IsEqualTo("a").ConfigureAwait(false);

            await Assert.That(flattened.Tuple[1].String).IsEqualTo("b").ConfigureAwait(false);

            await Assert.That(flattened.Tuple[2].Number).IsEqualTo(3).ConfigureAwait(false);

            await Assert.That(flattened.Tuple[3].Number).IsEqualTo(4).ConfigureAwait(false);

            await Assert.That(flattened.Tuple[4].String).IsEqualTo("tail").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleNestedThrowsWhenValuesNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                LuaValue.NewTupleNested((LuaValue[])null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("values").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleNestedReturnsSingleValueUnchanged()
        {
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewString("value"));

            LuaValue nested = LuaValue.NewTupleNested(tuple);

            await Assert.That(nested).IsEqualTo(tuple).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleThrowsWhenValuesNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                LuaValue.NewTuple((LuaValue[])null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("values").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleNestedWithSingleTuplePassesThrough()
        {
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue tuple = LuaValue.NewTuple(first, second);

            LuaValue nested = LuaValue.NewTupleNested(tuple);

            await Assert.That(nested).IsEqualTo(tuple).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleNestedWithoutTuplesCreatesRegularTuple()
        {
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewString("two");

            LuaValue result = LuaValue.NewTupleNested(first, second);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0]).IsEqualTo(first).ConfigureAwait(false);

            await Assert.That(result.Tuple[1]).IsEqualTo(second).ConfigureAwait(false);

            LuaValue[] copied = result.AsTuple();
            copied[0] = LuaValue.Nil;

            await Assert.That(result.Tuple[0]).IsEqualTo(first).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewTableFromArrayInitializesEntriesAndOwner()
        {
            Script script = new();
            LuaValue[] values = new[] { LuaValue.NewNumber(7), LuaValue.NewString("value") };

            LuaValue tableValue = LuaValue.NewTable(script, values);

            await Assert
                .That(tableValue.Table.OwnerScript)
                .IsSameReferenceAs(script)
                .ConfigureAwait(false);

            await Assert.That(tableValue.Table.Length).IsEqualTo(2).ConfigureAwait(false);

            await Assert.That(tableValue.Table.Get(1).Number).IsEqualTo(7).ConfigureAwait(false);

            await Assert
                .That(tableValue.Table.Get(2).String)
                .IsEqualTo("value")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToScalarReturnsFirstScalarEntry()
        {
            LuaValue nested = LuaValue.NewTuple(
                LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewNumber(2)),
                LuaValue.NewString("ignored")
            );

            LuaValue scalar = nested.ToScalar();

            await Assert.That(scalar.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);

            await Assert.That(scalar.Number).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CastToBoolRespectsLuaTruthinessRules()
        {
            await Assert.That(LuaValue.Nil.CastToBool()).IsFalse().ConfigureAwait(false);

            await Assert.That(LuaValue.Void.CastToBool()).IsFalse().ConfigureAwait(false);

            await Assert.That(LuaValue.False.CastToBool()).IsFalse().ConfigureAwait(false);

            await Assert
                .That(LuaValue.NewString("value").CastToBool())
                .IsTrue()
                .ConfigureAwait(false);

            await Assert.That(LuaValue.NewNumber(0).CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetLengthSupportsStringsAndTables()
        {
            LuaValue @string = LuaValue.NewString("abcd");
            Table table = new(null);
            table.Set(1, LuaValue.NewNumber(10));
            table.Set(2, LuaValue.NewNumber(20));
            LuaValue tableValue = LuaValue.NewTable(table);

            LuaValue stringLength = @string.GetLength();
            LuaValue tableLength = tableValue.GetLength();

            await Assert.That(stringLength.Number).IsEqualTo(4).ConfigureAwait(false);

            await Assert.That(tableLength.Number).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetLengthThrowsWhenTypeHasNoLength()
        {
            LuaValue number = LuaValue.NewNumber(5);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                number.GetLength()
            );

            await Assert.That(exception.Message).Contains("Can't get length").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0L)]
        [global::TUnit.Core.Arguments(1L)]
        [global::TUnit.Core.Arguments(127L)]
        [global::TUnit.Core.Arguments(255L)]
        public async Task FromIntegerReturnsCachedValueForSmallPositiveIntegers(long value)
        {
            LuaValue first = LuaValue.FromInteger(value);
            LuaValue second = LuaValue.FromInteger(value);

            await Assert.That(first).IsEqualTo(second).ConfigureAwait(false);
            await Assert.That(first.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(first.Number).IsEqualTo(value).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(-1L)]
        [global::TUnit.Core.Arguments(-127L)]
        [global::TUnit.Core.Arguments(-256L)]
        public async Task FromIntegerReturnsCachedValueForSmallNegativeIntegers(long value)
        {
            LuaValue first = LuaValue.FromInteger(value);
            LuaValue second = LuaValue.FromInteger(value);

            await Assert.That(first).IsEqualTo(second).ConfigureAwait(false);
            await Assert.That(first.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(first.Number).IsEqualTo(value).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(256L)]
        [global::TUnit.Core.Arguments(1000L)]
        [global::TUnit.Core.Arguments(-257L)]
        [global::TUnit.Core.Arguments(-1000L)]
        public async Task FromIntegerReturnsNewValueForOutOfCacheRange(long value)
        {
            LuaValue first = LuaValue.FromInteger(value);
            LuaValue second = LuaValue.FromInteger(value);

            await Assert.That(first).IsEqualTo(second).ConfigureAwait(false);
            await Assert.That(first.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(first.Number).IsEqualTo(value).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0.0)]
        [global::TUnit.Core.Arguments(1.0)]
        [global::TUnit.Core.Arguments(-1.0)]
        [global::TUnit.Core.Arguments(2.0)]
        [global::TUnit.Core.Arguments(0.5)]
        [global::TUnit.Core.Arguments(double.PositiveInfinity)]
        [global::TUnit.Core.Arguments(double.NegativeInfinity)]
        public async Task FromFloatReturnsCachedValueForCommonFloats(double value)
        {
            LuaValue first = LuaValue.FromFloat(value);
            LuaValue second = LuaValue.FromFloat(value);

            await Assert.That(first).IsEqualTo(second).ConfigureAwait(false);
            await Assert.That(first.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(first.Number).IsEqualTo(value).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(3.14159)]
        [global::TUnit.Core.Arguments(-0.333)]
        [global::TUnit.Core.Arguments(12345.6789)]
        public async Task FromFloatReturnsNewValueForUncommonFloats(double value)
        {
            LuaValue first = LuaValue.FromFloat(value);
            LuaValue second = LuaValue.FromFloat(value);

            await Assert.That(first).IsEqualTo(second).ConfigureAwait(false);
            await Assert.That(first.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(first.Number).IsEqualTo(value).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FromFloatPreservesFloatSubtypeForWholeNumbers()
        {
            LuaValue one = LuaValue.FromFloat(1.0);

            await Assert.That(one.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(one.IsInteger).IsFalse().ConfigureAwait(false);
            await Assert.That(one.Number).IsEqualTo(1.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FromIntegerPreservesIntegerSubtype()
        {
            LuaValue smallInteger = LuaValue.FromInteger(1);
            LuaValue largeInteger = LuaValue.FromInteger(1000);

            await Assert.That(smallInteger.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(smallInteger.IsFloat).IsFalse().ConfigureAwait(false);
            await Assert.That(largeInteger.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert.That(largeInteger.IsFloat).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeAutoConvertsNumbersToStrings()
        {
            LuaValue number = LuaValue.NewNumber(12.5);

            LuaValue converted = number.CheckType(
                "func",
                DataType.String,
                argNum: 0,
                flags: TypeValidationOptions.AutoConvert
            );

            await Assert.That(converted.Type).IsEqualTo(DataType.String).ConfigureAwait(false);

            await Assert.That(converted.String).IsEqualTo("12.5").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeThrowsWhenConversionNotAllowed()
        {
            LuaValue number = LuaValue.NewNumber(12.5);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                number.CheckType(
                    "func",
                    DataType.String,
                    argNum: 0,
                    flags: (TypeValidationOptions)0
                )
            );

            await Assert.That(exception.Message).Contains("bad argument #1").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetLengthThrowsOnUnsupportedTypes()
        {
            Script script = new();
            CallbackFunction callback = new CallbackFunction((_, _) => LuaValue.True);
            LuaValue function = LuaValue.NewCallback(callback);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                function.GetLength()
            );

            await Assert.That(exception.Message).Contains("Can't get length").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetAsPrivateResourceReturnsNullWhenNotPrivate()
        {
            LuaValue number = LuaValue.NewNumber(5);

            await Assert.That(number.ScriptPrivateResource).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetTypeConvertsValueToRequestedType()
        {
            LuaValue number = LuaValue.NewNumber(7);

            double converted = number.ToObject<double>();

            await Assert.That(converted).IsEqualTo(7d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TypeChecksThrowWhenTypeMissing()
        {
            LuaValue nil = LuaValue.Nil;

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                nil.CheckType("func", DataType.Number, argNum: 1)
            );

            await Assert.That(exception.Message).Contains("bad argument #2").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeThrowsWhenVoidAndValueRequired()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                LuaValue.Void.CheckType("func", DataType.Number, argNum: 2)
            );

            await Assert.That(exception.Message).Contains("got no value").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CastToNumberParsesInvariantStrings()
        {
            LuaValue numericString = LuaValue.NewString("12.75");
            double? result = numericString.CastToNumber();

            await Assert.That(result).IsEqualTo(12.75).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CastToNumberReturnsNullForNonNumericStrings()
        {
            await Assert
                .That(LuaValue.NewString("not-a-number").CastToNumber())
                .IsNull()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CastToStringConvertsNumbers()
        {
            LuaValue number = LuaValue.NewNumber(5.5);

            await Assert.That(number.CastToString()).IsEqualTo("5.5").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeAllowsNilWhenFlagSet()
        {
            LuaValue result = LuaValue.Nil.CheckType(
                "func",
                DataType.Table,
                flags: TypeValidationOptions.AllowNil
            );

            await Assert.That(result).IsEqualTo(LuaValue.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckUserDataTypeReturnsManagedInstance()
        {
            using UserDataRegistrationScope registrationScope = RegisterSampleUserData();
            bool created = UserData.TryCreate(new SampleUserData("ud"), out LuaValue userData);
            await Assert.That(created).IsTrue().ConfigureAwait(false);

            SampleUserData result = userData.CheckUserDataType<SampleUserData>("func");

            await Assert.That(result.Name).IsEqualTo("ud").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckUserDataTypeThrowsWhenTypeMismatch()
        {
            using UserDataRegistrationScope registrationScope = RegisterSampleUserData();
            bool created = UserData.TryCreate(new SampleUserData("ud"), out LuaValue userData);
            await Assert.That(created).IsTrue().ConfigureAwait(false);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                userData.CheckUserDataType<string>("func")
            );

            await Assert.That(exception.Message).Contains("userdata").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckUserDataTypeAllowsNilWhenFlagged()
        {
            SampleUserData result = LuaValue.Nil.CheckUserDataType<SampleUserData>(
                "func",
                flags: TypeValidationOptions.AllowNil
            );

            await Assert.That(result).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFromStringBuilderCopiesSnapshot()
        {
            StringBuilder builder = new("seed");
            LuaValue value = LuaValue.NewString(builder);
            builder.Append("mutated");

            await Assert.That(value.String).IsEqualTo("seed").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFromStringBuilderThrowsWhenBuilderIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                LuaValue.NewString((StringBuilder)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("sb").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFormatThrowsWhenFormatIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                LuaValue.NewString(null, "value")
            );

            await Assert.That(exception.ParamName).IsEqualTo("format").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFormatAppliesArguments()
        {
            LuaValue value = LuaValue.NewString("value {0} {1}", 5, "x");

            await Assert.That(value.String).IsEqualTo("value 5 x").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFormatReturnsLiteralWhenArgsNull()
        {
            LuaValue value = LuaValue.NewString("literal", (object[])null);

            await Assert.That(value.String).IsEqualTo("literal").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NewCoroutineWrapsCoroutineHandles(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.Call(
                script.LoadString("return function(x) coroutine.yield(x); return x end")
            );
            LuaValue coroutineValue = script.CreateCoroutineValue(function);
            LuaValue wrapped = LuaValue.NewCoroutine(coroutineValue.Coroutine);

            await Assert.That(wrapped.Type).IsEqualTo(DataType.Thread).ConfigureAwait(false);

            await Assert
                .That(wrapped.Coroutine)
                .IsSameReferenceAs(coroutineValue.Coroutine)
                .ConfigureAwait(false);

            await Assert.That(wrapped.ToRawString()).Contains("Coroutine").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToStringFormatsClrFunctions()
        {
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil, "named");
            await Assert
                .That(callback.ToRawString())
                .IsEqualTo("(Function CLR)")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToStringCoversLuaTypeRepresentations()
        {
            Script script = new();
            LuaValue chunk = script.LoadString("return function() return 1 end");
            LuaValue coroutine = script.CreateCoroutineValue(chunk);
            LuaValue tableValue = LuaValue.NewTable(new Table(script));
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewString("two"));
            using UserDataRegistrationScope registrationScope = RegisterSampleUserData();
            bool created = UserData.TryCreate(new SampleUserData("ignored"), out LuaValue userData);
            await Assert.That(created).IsTrue().ConfigureAwait(false);
            LuaValue yield = LuaValue.NewYieldReq(Array.Empty<LuaValue>());

            await Assert.That(LuaValue.Void.ToRawString()).IsEqualTo("void").ConfigureAwait(false);

            await Assert.That(chunk.ToRawString()).StartsWith("(Function ").ConfigureAwait(false);

            await Assert.That(tableValue.ToRawString()).IsEqualTo("(Table)").ConfigureAwait(false);

            await Assert.That(tuple.ToRawString()).IsEqualTo("1, \"two\"").ConfigureAwait(false);

            await Assert.That(userData.ToRawString()).IsEqualTo("(UserData)").ConfigureAwait(false);

            await Assert
                .That(coroutine.ToRawString())
                .StartsWith("(Coroutine ")
                .ConfigureAwait(false);

            await Assert.That(yield.ToRawString()).IsEqualTo("(???)").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeAutoConvertsAcrossCoreTypes()
        {
            LuaValue boolValue = LuaValue
                .NewString("truthy")
                .CheckType("func", DataType.Boolean, flags: TypeValidationOptions.AutoConvert);
            LuaValue numberValue = LuaValue
                .NewString("42")
                .CheckType("func", DataType.Number, flags: TypeValidationOptions.AutoConvert);
            LuaValue stringValue = LuaValue
                .NewNumber(3.5)
                .CheckType("func", DataType.String, flags: TypeValidationOptions.AutoConvert);

            await Assert.That(boolValue.Boolean).IsTrue().ConfigureAwait(false);

            await Assert.That(numberValue.Number).IsEqualTo(42).ConfigureAwait(false);

            await Assert.That(stringValue.String).IsEqualTo("3.5").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeComplainsWhenVoidHasNoValue()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                LuaValue.Void.CheckType("func", DataType.String)
            );

            await Assert.That(exception.Message).Contains("no value").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeAutoConvertFallbacksReturnOriginalWhenConversionFails()
        {
            LuaValue original = LuaValue.NewString("not-number");
            LuaValue result = original.CheckType(
                "func",
                DataType.String,
                flags: TypeValidationOptions.AutoConvert
            );

            await Assert.That(result).IsEqualTo(original).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckUserDataTypeReturnsDefaultWhenNilAllowed()
        {
            SampleUserData result = LuaValue.Nil.CheckUserDataType<SampleUserData>(
                "func",
                flags: TypeValidationOptions.AllowNil
            );

            await Assert.That(result).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToPrintStringReflectsUserDataDescriptor()
        {
            using UserDataRegistrationScope registrationScope = RegisterSampleUserData();
            bool created = UserData.TryCreate(
                new SampleUserData("Printable"),
                out LuaValue userData
            );
            await Assert.That(created).IsTrue().ConfigureAwait(false);

            await Assert
                .That(userData.ToPrintString())
                .IsEqualTo("Printable")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToPrintStringFormatsCompositeValues()
        {
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewString("a"), LuaValue.NewNumber(5));
            LuaValue tail = LuaValue.NewTailCallReq(
                LuaValue.NewCallback((_, _) => LuaValue.Nil),
                LuaValue.NewNumber(1)
            );
            LuaValue yield = LuaValue.NewYieldReq(Array.Empty<LuaValue>());

            await Assert.That(tuple.ToPrintString()).IsEqualTo("a\t5").ConfigureAwait(false);

            await Assert
                .That(tail.ToPrintString())
                .IsEqualTo("(TailCallRequest -- INTERNAL!)")
                .ConfigureAwait(false);

            await Assert
                .That(yield.ToPrintString())
                .IsEqualTo("(YieldRequest -- INTERNAL!)")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToPrintStringFallsBackToRefIdForTablesAndUserData()
        {
            Script script = new();
            LuaValue tableValue = LuaValue.NewTable(new Table(script));
            LuaValue userData = UserData.Create(new object(), new NullStringDescriptor());

            await Assert
                .That(tableValue.ToPrintString())
                .StartsWith("table: ")
                .ConfigureAwait(false);

            await Assert
                .That(userData.ToPrintString())
                .StartsWith("userdata: ")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetHashCodeIsStableForUnchangedValue()
        {
            LuaValue str = LuaValue.NewString("hash-me");
            LuaValue integerZero = LuaValue.NewInteger(0);
            LuaValue positiveZero = LuaValue.NewFloat(0.0);
            LuaValue negativeZero = LuaValue.NewFloat(-0.0);

            int first = str.GetHashCode();
            int second = str.GetHashCode();

            await Assert.That(second).IsEqualTo(first).ConfigureAwait(false);
            await Assert.That(integerZero.Equals(positiveZero)).IsTrue().ConfigureAwait(false);
            await Assert.That(integerZero.Equals(negativeZero)).IsTrue().ConfigureAwait(false);
            await Assert
                .That(integerZero.GetHashCode())
                .IsEqualTo(positiveZero.GetHashCode())
                .ConfigureAwait(false);
            await Assert
                .That(integerZero.GetHashCode())
                .IsEqualTo(negativeZero.GetHashCode())
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetHashCodeHandlesNilAndTupleCases()
        {
            LuaValue defaultValue = default;
            int nilHash = LuaValue.Nil.GetHashCode();
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewNumber(2));
            int tupleHash = tuple.GetHashCode();

            await Assert.That(defaultValue.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(defaultValue).IsEqualTo(LuaValue.Nil).ConfigureAwait(false);
            await Assert.That(LuaValue.Void.Type).IsEqualTo(DataType.Void).ConfigureAwait(false);
            await Assert.That(LuaValue.Void).IsEqualTo(LuaValue.Nil).ConfigureAwait(false);
            await Assert.That(nilHash).IsEqualTo(LuaValue.Nil.GetHashCode()).ConfigureAwait(false);
            await Assert.That(nilHash).IsEqualTo(LuaValue.Void.GetHashCode()).ConfigureAwait(false);

            await Assert.That(tupleHash).IsEqualTo(tuple.GetHashCode()).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EqualsHandlesNonDynValuesTuplesUserDataAndYieldRequests()
        {
            using UserDataRegistrationScope registrationScope = RegisterSampleUserData();
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewNumber(2));
            LuaValue alias = LuaValue.NewTuple(tuple.Tuple);
            LuaValue tupleCopy = LuaValue.NewTuple(LuaValue.NewNumber(1), LuaValue.NewNumber(2));
            LuaValue nullUserData = LuaValue.NewUserData(null);
            bool createdUserData = UserData.TryCreate(
                new SampleUserData("value"),
                out LuaValue userData
            );
            LuaValue forcedYield = LuaValue.NewForcedYieldReq();
            LuaValue separateForcedYield = LuaValue.NewForcedYieldReq();
            LuaValue nan = LuaValue.NewFloat(double.NaN);
            Table table = new(null);
            LuaValue tableValue = LuaValue.NewTable(table);
            LuaValue tableAlias = LuaValue.NewTable(table);
            LuaValue separateTable = LuaValue.NewTable(new Table(null));
            SampleUserData managed = new("shared");
            bool createdFirstWrapper = UserData.TryCreate(
                managed,
                out LuaValue firstManagedWrapper
            );
            bool createdSecondWrapper = UserData.TryCreate(
                managed,
                out LuaValue secondManagedWrapper
            );

            await Assert.That(createdUserData).IsTrue().ConfigureAwait(false);
            await Assert.That(createdFirstWrapper).IsTrue().ConfigureAwait(false);
            await Assert.That(createdSecondWrapper).IsTrue().ConfigureAwait(false);

            await Assert.That(tuple.Equals("value")).IsFalse().ConfigureAwait(false);

            await Assert.That(tuple.Equals(alias)).IsTrue().ConfigureAwait(false);

            await Assert.That(tuple.Equals(tupleCopy)).IsFalse().ConfigureAwait(false);

            await Assert.That(nullUserData.Equals(userData)).IsFalse().ConfigureAwait(false);

            await Assert.That(forcedYield.Equals(forcedYield)).IsTrue().ConfigureAwait(false);
            await Assert
                .That(forcedYield.Equals(separateForcedYield))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert.That(nan.Equals(nan)).IsFalse().ConfigureAwait(false);
            await Assert.That(tableValue.Equals(tableAlias)).IsTrue().ConfigureAwait(false);
            await Assert.That(tableValue.Equals(separateTable)).IsFalse().ConfigureAwait(false);
            await Assert
                .That(tableValue.HasSameReferenceIdentity(tableAlias))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(tableValue.HasSameReferenceIdentity(separateTable))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(firstManagedWrapper.Equals(secondManagedWrapper))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(firstManagedWrapper.HasSameReferenceIdentity(secondManagedWrapper))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToDebugPrintStringFlattensTuples()
        {
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewString("x"), LuaValue.NewNumber(4));

            await Assert.That(tuple.ToDebugPrintString()).IsEqualTo("x\t4").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToDebugPrintStringDisplaysTailYieldAndScalars()
        {
            LuaValue tail = LuaValue.NewTailCallReq(
                LuaValue.NewCallback((_, _) => LuaValue.Nil),
                LuaValue.NewNumber(9)
            );
            LuaValue yield = LuaValue.NewYieldReq(Array.Empty<LuaValue>());

            await Assert
                .That(tail.ToDebugPrintString())
                .IsEqualTo("(TailCallRequest)")
                .ConfigureAwait(false);

            await Assert
                .That(yield.ToDebugPrintString())
                .IsEqualTo("(YieldRequest)")
                .ConfigureAwait(false);

            await Assert
                .That(LuaValue.True.ToDebugPrintString())
                .IsEqualTo(LuaValue.True.ToString())
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToDebugPrintStringUsesFormatTypeStringWhenAsStringReturnsNull()
        {
            NullStringDescriptor descriptor = new();
            LuaValue userDataValue = UserData.Create(new object(), descriptor);

            string debugString = userDataValue.ToDebugPrintString();

            await Assert.That(debugString).StartsWith("userdata:").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetHashCodeHandlesBooleanValues()
        {
            int trueHash = LuaValue.True.GetHashCode();
            int trueHash2 = LuaValue.True.GetHashCode();
            int falseHash = LuaValue.False.GetHashCode();
            int falseHash2 = LuaValue.False.GetHashCode();

            // Same value should produce consistent hash code
            await Assert.That(trueHash).IsEqualTo(trueHash2).ConfigureAwait(false);
            await Assert.That(falseHash).IsEqualTo(falseHash2).ConfigureAwait(false);

            // Different boolean values should have different hash codes
            await Assert.That(trueHash).IsNotEqualTo(falseHash).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsNilOrNanDetectsNaN()
        {
            LuaValue value = LuaValue.NewNumber(double.NaN);
            await Assert.That(value.IsNilOrNan()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsNotVoidDistinguishesVoidValues()
        {
            await Assert.That(LuaValue.Void.IsNotVoid()).IsFalse().ConfigureAwait(false);

            await Assert.That(LuaValue.NewNumber(1).IsNotVoid()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetAsPrivateResourceReturnsUnderlyingResource()
        {
            Script script = new();
            Table table = new(script);
            LuaValue tableValue = LuaValue.NewTable(table);

            await Assert
                .That(tableValue.ScriptPrivateResource)
                .IsSameReferenceAs(table)
                .ConfigureAwait(false);
        }

        private static UserDataRegistrationScope RegisterSampleUserData()
        {
            UserDataRegistrationScope scope = UserDataRegistrationScope.Track<SampleUserData>(
                ensureUnregistered: true
            );
            scope.RegisterType<SampleUserData>();
            return scope;
        }

        private sealed class SampleUserData
        {
            public SampleUserData(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public override string ToString()
            {
                return Name;
            }
        }

        private sealed class NullStringDescriptor : IUserDataDescriptor
        {
            public string Name => "NullPrinter";

            public Type Type => typeof(object);

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
                return null;
            }

            public bool TryMetaIndex(Script script, object obj, string metaname, out LuaValue value)
            {
                value = LuaValue.Nil;
                return false;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                if (obj == null)
                {
                    return true;
                }

                return type.IsInstanceOfType(obj);
            }
        }
    }
}
