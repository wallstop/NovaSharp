#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;

    public sealed class DynValueTUnitTests
    {
        static DynValueTUnitTests()
        {
            UserData.RegisterType<SampleUserData>();
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleHandlesEmptyAndSingleInputs()
        {
            DynValue empty = DynValue.NewTuple();
            DynValue single = DynValue.NewNumber(42);
            DynValue wrappedSingle = DynValue.NewTuple(single);

            await Assert.That(empty.Type).IsEqualTo(DataType.Nil);

            await Assert.That(wrappedSingle).IsSameReferenceAs(single);
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleNestedFlattensTuplesOneLevelDeep()
        {
            DynValue tupleA = DynValue.NewTuple(DynValue.NewString("a"), DynValue.NewString("b"));
            DynValue tupleB = DynValue.NewTuple(DynValue.NewNumber(3), DynValue.NewNumber(4));

            DynValue flattened = DynValue.NewTupleNested(
                tupleA,
                tupleB,
                DynValue.NewString("tail")
            );

            await Assert.That(flattened.Type).IsEqualTo(DataType.Tuple);

            await Assert.That(flattened.Tuple.Length).IsEqualTo(5);
            await Assert.That(flattened.Tuple[0].String).IsEqualTo("a");

            await Assert.That(flattened.Tuple[1].String).IsEqualTo("b");

            await Assert.That(flattened.Tuple[2].Number).IsEqualTo(3);

            await Assert.That(flattened.Tuple[3].Number).IsEqualTo(4);

            await Assert.That(flattened.Tuple[4].String).IsEqualTo("tail");
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleNestedThrowsWhenValuesNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                DynValue.NewTupleNested((DynValue[])null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("values");
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleNestedReturnsSingleValueUnchanged()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewString("value"));

            DynValue nested = DynValue.NewTupleNested(tuple);

            await Assert.That(nested).IsSameReferenceAs(tuple);
        }

        [global::TUnit.Core.Test]
        public async Task NewTupleNestedWithoutTuplesCreatesRegularTuple()
        {
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewString("two");

            DynValue result = DynValue.NewTupleNested(first, second);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0]).IsSameReferenceAs(first);

            await Assert.That(result.Tuple[1]).IsSameReferenceAs(second);
        }

        [global::TUnit.Core.Test]
        public async Task NewTableFromArrayInitializesEntriesAndOwner()
        {
            Script script = new();
            DynValue[] values = new[] { DynValue.NewNumber(7), DynValue.NewString("value") };

            DynValue tableValue = DynValue.NewTable(script, values);

            await Assert.That(tableValue.Table.OwnerScript).IsSameReferenceAs(script);

            await Assert.That(tableValue.Table.Length).IsEqualTo(2);

            await Assert.That(tableValue.Table.Get(1).Number).IsEqualTo(7);

            await Assert.That(tableValue.Table.Get(2).String).IsEqualTo("value");
        }

        [global::TUnit.Core.Test]
        public async Task ToScalarReturnsFirstScalarEntry()
        {
            DynValue nested = DynValue.NewTuple(
                DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2)),
                DynValue.NewString("ignored")
            );

            DynValue scalar = nested.ToScalar();

            await Assert.That(scalar.Type).IsEqualTo(DataType.Number);

            await Assert.That(scalar.Number).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task CastToBoolRespectsLuaTruthinessRules()
        {
            await Assert.That(DynValue.Nil.CastToBool()).IsFalse();

            await Assert.That(DynValue.Void.CastToBool()).IsFalse();

            await Assert.That(DynValue.False.CastToBool()).IsFalse();

            await Assert.That(DynValue.NewString("value").CastToBool()).IsTrue();

            await Assert.That(DynValue.NewNumber(0).CastToBool()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetLengthSupportsStringsAndTables()
        {
            DynValue @string = DynValue.NewString("abcd");
            Table table = new(null);
            table.Set(1, DynValue.NewNumber(10));
            table.Set(2, DynValue.NewNumber(20));
            DynValue tableValue = DynValue.NewTable(table);

            DynValue stringLength = @string.GetLength();
            DynValue tableLength = tableValue.GetLength();

            await Assert.That(stringLength.Number).IsEqualTo(4);

            await Assert.That(tableLength.Number).IsEqualTo(2);
        }

        [global::TUnit.Core.Test]
        public async Task GetLengthThrowsWhenTypeHasNoLength()
        {
            DynValue number = DynValue.NewNumber(5);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                number.GetLength()
            );

            await Assert.That(exception.Message).Contains("Can't get length");
        }

        [global::TUnit.Core.Test]
        public async Task AssignCopiesWritableValuesAndResetsHashcode()
        {
            DynValue destination = DynValue.NewNumber(1);
            _ = destination.GetHashCode(); // populate cached hash

            DynValue source = DynValue.NewString("hello");
            destination.Assign(source);

            await Assert.That(destination.Type).IsEqualTo(DataType.String);

            await Assert.That(destination.String).IsEqualTo("hello");

            await Assert.That(destination.GetHashCode()).IsEqualTo(source.GetHashCode());
        }

        [global::TUnit.Core.Test]
        public async Task AssignThrowsWhenValueIsReadOnly()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                DynValue.True.Assign(DynValue.False)
            );

            await Assert.That(exception.Message).Contains("Assigning on r-value");
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeAutoConvertsNumbersToStrings()
        {
            DynValue number = DynValue.NewNumber(12.5);

            DynValue converted = number.CheckType(
                "func",
                DataType.String,
                argNum: 0,
                flags: TypeValidationOptions.AutoConvert
            );

            await Assert.That(converted.Type).IsEqualTo(DataType.String);

            await Assert.That(converted.String).IsEqualTo("12.5");
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeThrowsWhenConversionNotAllowed()
        {
            DynValue number = DynValue.NewNumber(12.5);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                number.CheckType(
                    "func",
                    DataType.String,
                    argNum: 0,
                    flags: (TypeValidationOptions)0
                )
            );

            await Assert.That(exception.Message).Contains("bad argument #1");
        }

        [global::TUnit.Core.Test]
        public async Task GetLengthThrowsOnUnsupportedTypes()
        {
            Script script = new();
            CallbackFunction callback = new CallbackFunction((_, _) => DynValue.True);
            DynValue function = DynValue.NewCallback(callback);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                function.GetLength()
            );

            await Assert.That(exception.Message).Contains("Can't get length");
        }

        [global::TUnit.Core.Test]
        public async Task AssignCopiesObjectReferencesForTables()
        {
            Script script = new();
            Table table = new(script);
            DynValue destination = DynValue.NewNil();

            destination.Assign(DynValue.NewTable(table));

            await Assert.That(destination.Type).IsEqualTo(DataType.Table);

            await Assert.That(destination.Table).IsSameReferenceAs(table);
        }

        [global::TUnit.Core.Test]
        public async Task AssignPreservesReadOnlyForDestination()
        {
            DynValue destination = DynValue.NewNumber(1).AsReadOnly();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                destination.Assign(DynValue.NewNumber(2))
            );

            await Assert.That(exception.Message).Contains("Assigning on r-value");
        }

        [global::TUnit.Core.Test]
        public async Task AssignNumberThrowsWhenDestinationReadOnly()
        {
            DynValue destination = DynValue.NewNumber(1).AsReadOnly();

            Assert.Throws<InternalErrorException>(() => destination.AssignNumber(2));
        }

        [global::TUnit.Core.Test]
        public async Task AssignNumberThrowsWhenDestinationIsNotNumeric()
        {
            DynValue destination = DynValue.NewString("value");

            Assert.Throws<InternalErrorException>(() => destination.AssignNumber(2));
        }

        [global::TUnit.Core.Test]
        public async Task GetAsPrivateResourceReturnsNullWhenNotPrivate()
        {
            DynValue number = DynValue.NewNumber(5);

            await Assert.That(number.ScriptPrivateResource).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task GetTypeConvertsValueToRequestedType()
        {
            DynValue number = DynValue.NewNumber(7);

            double converted = number.ToObject<double>();

            await Assert.That(converted).IsEqualTo(7d);
        }

        [global::TUnit.Core.Test]
        public async Task TypeChecksThrowWhenTypeMissing()
        {
            DynValue nil = DynValue.Nil;

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                nil.CheckType("func", DataType.Number, argNum: 1)
            );

            await Assert.That(exception.Message).Contains("bad argument #2");
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeThrowsWhenVoidAndValueRequired()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                DynValue.Void.CheckType("func", DataType.Number, argNum: 2)
            );

            await Assert.That(exception.Message).Contains("got no value");
        }

        [global::TUnit.Core.Test]
        public async Task CastToNumberParsesInvariantStrings()
        {
            DynValue numericString = DynValue.NewString("12.75");
            double? result = numericString.CastToNumber();

            await Assert.That(result).IsEqualTo(12.75);
        }

        [global::TUnit.Core.Test]
        public async Task CastToNumberReturnsNullForNonNumericStrings()
        {
            await Assert.That(DynValue.NewString("not-a-number").CastToNumber()).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task CastToStringConvertsNumbers()
        {
            DynValue number = DynValue.NewNumber(5.5);

            await Assert.That(number.CastToString()).IsEqualTo("5.5");
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeAllowsNilWhenFlagSet()
        {
            DynValue result = DynValue.Nil.CheckType(
                "func",
                DataType.Table,
                flags: TypeValidationOptions.AllowNil
            );

            await Assert.That(result).IsSameReferenceAs(DynValue.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task CheckUserDataTypeReturnsManagedInstance()
        {
            DynValue userData = UserData.Create(new SampleUserData("ud"));

            SampleUserData result = userData.CheckUserDataType<SampleUserData>("func");

            await Assert.That(result.Name).IsEqualTo("ud");
        }

        [global::TUnit.Core.Test]
        public async Task CheckUserDataTypeThrowsWhenTypeMismatch()
        {
            DynValue userData = UserData.Create(new SampleUserData("ud"));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                userData.CheckUserDataType<string>("func")
            );

            await Assert.That(exception.Message).Contains("userdata");
        }

        [global::TUnit.Core.Test]
        public async Task CheckUserDataTypeAllowsNilWhenFlagged()
        {
            SampleUserData result = DynValue.Nil.CheckUserDataType<SampleUserData>(
                "func",
                flags: TypeValidationOptions.AllowNil
            );

            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFromStringBuilderCopiesSnapshot()
        {
            StringBuilder builder = new("seed");
            DynValue value = DynValue.NewString(builder);
            builder.Append("mutated");

            await Assert.That(value.String).IsEqualTo("seed");
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFromStringBuilderThrowsWhenBuilderIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                DynValue.NewString((StringBuilder)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("sb");
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFormatThrowsWhenFormatIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                DynValue.NewString(null, "value")
            );

            await Assert.That(exception.ParamName).IsEqualTo("format");
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFormatAppliesArguments()
        {
            DynValue value = DynValue.NewString("value {0} {1}", 5, "x");

            await Assert.That(value.String).IsEqualTo("value 5 x");
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFormatReturnsLiteralWhenArgsNull()
        {
            DynValue value = DynValue.NewString("literal", (object[])null);

            await Assert.That(value.String).IsEqualTo("literal");
        }

        [global::TUnit.Core.Test]
        public async Task CloneReflectsReadOnlyPreference()
        {
            DynValue number = DynValue.NewNumber(7);
            DynValue readOnly = number.Clone(true);
            DynValue writable = readOnly.Clone(false);

            await Assert.That(readOnly.ReadOnly).IsTrue();

            await Assert.That(writable.ReadOnly).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task CloneAsWritableProducesEditableCopy()
        {
            DynValue readOnly = DynValue.NewString("locked").AsReadOnly();
            DynValue clone = readOnly.CloneAsWritable();

            clone.Assign(DynValue.NewString("unlocked"));

            await Assert.That(clone.String).IsEqualTo("unlocked");

            await Assert.That(readOnly.String).IsEqualTo("locked");
        }

        [global::TUnit.Core.Test]
        public async Task AssignUpdatesTargetAndResetsHashCode()
        {
            DynValue target = DynValue.NewNumber(1);
            int oldHash = target.GetHashCode();

            target.Assign(DynValue.NewString("assigned"));

            await Assert.That(target.Type).IsEqualTo(DataType.String);

            await Assert.That(target.String).IsEqualTo("assigned");

            await Assert.That(target.GetHashCode()).IsNotEqualTo(oldHash);
        }

        [global::TUnit.Core.Test]
        public async Task AssignThrowsWhenTargetIsReadOnly()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                DynValue.True.Assign(DynValue.NewNumber(2))
            );

            await Assert.That(exception.Message).Contains("Assigning on r-value");
        }

        [global::TUnit.Core.Test]
        public async Task NewCoroutineWrapsCoroutineHandles()
        {
            Script script = new();
            DynValue function = script.Call(
                script.LoadString("return function(x) coroutine.yield(x); return x end")
            );
            DynValue coroutineValue = script.CreateCoroutine(function);
            DynValue wrapped = DynValue.NewCoroutine(coroutineValue.Coroutine);

            await Assert.That(wrapped.Type).IsEqualTo(DataType.Thread);

            await Assert.That(wrapped.Coroutine).IsSameReferenceAs(coroutineValue.Coroutine);

            await Assert.That(wrapped.ToString()).Contains("Coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task ToStringFormatsClrFunctions()
        {
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil, "named");
            await Assert.That(callback.ToString()).IsEqualTo("(Function CLR)");
        }

        [global::TUnit.Core.Test]
        public async Task ToStringCoversLuaTypeRepresentations()
        {
            Script script = new();
            DynValue chunk = script.LoadString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(chunk);
            DynValue tableValue = DynValue.NewTable(new Table(script));
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewString("two"));
            DynValue userData = UserData.Create(new SampleUserData("ignored"));
            DynValue yield = DynValue.NewYieldReq(Array.Empty<DynValue>());

            await Assert.That(DynValue.Void.ToString()).IsEqualTo("void");

            await Assert.That(chunk.ToString()).StartsWith("(Function ");

            await Assert.That(tableValue.ToString()).IsEqualTo("(Table)");

            await Assert.That(tuple.ToString()).IsEqualTo("1, \"two\"");

            await Assert.That(userData.ToString()).IsEqualTo("(UserData)");

            await Assert.That(coroutine.ToString()).StartsWith("(Coroutine ");

            await Assert.That(yield.ToString()).IsEqualTo("(???)");
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeAutoConvertsAcrossCoreTypes()
        {
            DynValue boolValue = DynValue
                .NewString("truthy")
                .CheckType("func", DataType.Boolean, flags: TypeValidationOptions.AutoConvert);
            DynValue numberValue = DynValue
                .NewString("42")
                .CheckType("func", DataType.Number, flags: TypeValidationOptions.AutoConvert);
            DynValue stringValue = DynValue
                .NewNumber(3.5)
                .CheckType("func", DataType.String, flags: TypeValidationOptions.AutoConvert);

            await Assert.That(boolValue.Boolean).IsTrue();

            await Assert.That(numberValue.Number).IsEqualTo(42);

            await Assert.That(stringValue.String).IsEqualTo("3.5");
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeComplainsWhenVoidHasNoValue()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                DynValue.Void.CheckType("func", DataType.String)
            );

            await Assert.That(exception.Message).Contains("no value");
        }

        [global::TUnit.Core.Test]
        public async Task CheckTypeAutoConvertFallbacksReturnOriginalWhenConversionFails()
        {
            DynValue original = DynValue.NewString("not-number");
            DynValue result = original.CheckType(
                "func",
                DataType.String,
                flags: TypeValidationOptions.AutoConvert
            );

            await Assert.That(result).IsSameReferenceAs(original);
        }

        [global::TUnit.Core.Test]
        public async Task CheckUserDataTypeReturnsDefaultWhenNilAllowed()
        {
            SampleUserData result = DynValue.Nil.CheckUserDataType<SampleUserData>(
                "func",
                flags: TypeValidationOptions.AllowNil
            );

            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task ToPrintStringReflectsUserDataDescriptor()
        {
            DynValue userData = UserData.Create(new SampleUserData("Printable"));

            await Assert.That(userData.ToPrintString()).IsEqualTo("Printable");
        }

        [global::TUnit.Core.Test]
        public async Task ToPrintStringFormatsCompositeValues()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewString("a"), DynValue.NewNumber(5));
            DynValue tail = DynValue.NewTailCallReq(
                DynValue.NewCallback((_, _) => DynValue.Nil),
                DynValue.NewNumber(1)
            );
            DynValue yield = DynValue.NewYieldReq(Array.Empty<DynValue>());

            await Assert.That(tuple.ToPrintString()).IsEqualTo("a\t5");

            await Assert.That(tail.ToPrintString()).IsEqualTo("(TailCallRequest -- INTERNAL!)");

            await Assert.That(yield.ToPrintString()).IsEqualTo("(YieldRequest -- INTERNAL!)");
        }

        [global::TUnit.Core.Test]
        public async Task ToPrintStringFallsBackToRefIdForTablesAndUserData()
        {
            Script script = new();
            DynValue tableValue = DynValue.NewTable(new Table(script));
            DynValue userData = UserData.Create(new object(), new NullStringDescriptor());

            await Assert.That(tableValue.ToPrintString()).StartsWith("table: ");

            await Assert.That(userData.ToPrintString()).StartsWith("userdata: ");
        }

        [global::TUnit.Core.Test]
        public async Task AsReadOnlyReturnsSameInstanceWhenAlreadyReadOnly()
        {
            await Assert.That(DynValue.True.AsReadOnly()).IsSameReferenceAs(DynValue.True);
        }

        [global::TUnit.Core.Test]
        public async Task GetHashCodeCachesPerInstance()
        {
            DynValue str = DynValue.NewString("hash-me");

            int first = str.GetHashCode();
            int second = str.GetHashCode();

            await Assert.That(second).IsEqualTo(first);
        }

        [global::TUnit.Core.Test]
        public async Task GetHashCodeHandlesNilAndTupleCases()
        {
            int nilHash = DynValue.Nil.GetHashCode();
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2));
            int tupleHash = tuple.GetHashCode();

            await Assert.That(nilHash).IsEqualTo(DynValue.Nil.GetHashCode());

            await Assert.That(tupleHash).IsEqualTo(tuple.GetHashCode());
        }

        [global::TUnit.Core.Test]
        public async Task EqualsHandlesNonDynValuesTuplesUserDataAndYieldRequests()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2));
            DynValue alias = DynValue.NewNil();
            alias.Assign(tuple);
            DynValue tupleCopy = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2));
            DynValue nullUserData = DynValue.NewUserData(null);
            DynValue userData = UserData.Create(new SampleUserData("value"));
            DynValue forcedYield = DynValue.NewForcedYieldReq();

            await Assert.That(tuple.Equals("value")).IsFalse();

            await Assert.That(tuple.Equals(alias)).IsTrue();

            await Assert.That(tuple.Equals(tupleCopy)).IsFalse();

            await Assert.That(nullUserData.Equals(userData)).IsFalse();

            await Assert.That(forcedYield.Equals(forcedYield)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ToDebugPrintStringFlattensTuples()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewString("x"), DynValue.NewNumber(4));

            await Assert.That(tuple.ToDebugPrintString()).IsEqualTo("x\t4");
        }

        [global::TUnit.Core.Test]
        public async Task ToDebugPrintStringDisplaysTailYieldAndScalars()
        {
            DynValue tail = DynValue.NewTailCallReq(
                DynValue.NewCallback((_, _) => DynValue.Nil),
                DynValue.NewNumber(9)
            );
            DynValue yield = DynValue.NewYieldReq(Array.Empty<DynValue>());

            await Assert.That(tail.ToDebugPrintString()).IsEqualTo("(TailCallRequest)");

            await Assert.That(yield.ToDebugPrintString()).IsEqualTo("(YieldRequest)");

            await Assert
                .That(DynValue.True.ToDebugPrintString())
                .IsEqualTo(DynValue.True.ToString());
        }

        [global::TUnit.Core.Test]
        public async Task IsNilOrNanDetectsNaN()
        {
            DynValue value = DynValue.NewNumber(double.NaN);
            await Assert.That(value.IsNilOrNan()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task IsNotVoidDistinguishesVoidValues()
        {
            await Assert.That(DynValue.Void.IsNotVoid()).IsFalse();

            await Assert.That(DynValue.NewNumber(1).IsNotVoid()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetAsPrivateResourceReturnsUnderlyingResource()
        {
            Script script = new();
            Table table = new(script);
            DynValue tableValue = DynValue.NewTable(table);

            await Assert.That(tableValue.ScriptPrivateResource).IsSameReferenceAs(table);
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
                return null;
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                return null;
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

#pragma warning restore CA2007
