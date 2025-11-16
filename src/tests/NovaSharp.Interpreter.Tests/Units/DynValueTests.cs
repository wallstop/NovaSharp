namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DynValueTests
    {
        [OneTimeSetUp]
        public void RegisterUserData()
        {
            UserData.RegisterType<SampleUserData>();
        }

        [Test]
        public void NewTupleHandlesEmptyAndSingleInputs()
        {
            DynValue empty = DynValue.NewTuple();
            DynValue single = DynValue.NewNumber(42);
            DynValue wrappedSingle = DynValue.NewTuple(single);

            Assert.Multiple(() =>
            {
                Assert.That(empty.Type, Is.EqualTo(DataType.Nil));
                Assert.That(wrappedSingle, Is.SameAs(single));
            });
        }

        [Test]
        public void NewTupleNestedFlattensTuplesOneLevelDeep()
        {
            DynValue tupleA = DynValue.NewTuple(DynValue.NewString("a"), DynValue.NewString("b"));
            DynValue tupleB = DynValue.NewTuple(DynValue.NewNumber(3), DynValue.NewNumber(4));

            DynValue flattened = DynValue.NewTupleNested(
                tupleA,
                tupleB,
                DynValue.NewString("tail")
            );

            Assert.Multiple(() =>
            {
                Assert.That(flattened.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(flattened.Tuple, Has.Length.EqualTo(5));
                Assert.That(flattened.Tuple[0].String, Is.EqualTo("a"));
                Assert.That(flattened.Tuple[1].String, Is.EqualTo("b"));
                Assert.That(flattened.Tuple[2].Number, Is.EqualTo(3));
                Assert.That(flattened.Tuple[3].Number, Is.EqualTo(4));
                Assert.That(flattened.Tuple[4].String, Is.EqualTo("tail"));
            });
        }

        [Test]
        public void ToScalarReturnsFirstScalarEntry()
        {
            DynValue nested = DynValue.NewTuple(
                DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2)),
                DynValue.NewString("ignored")
            );

            DynValue scalar = nested.ToScalar();

            Assert.Multiple(() =>
            {
                Assert.That(scalar.Type, Is.EqualTo(DataType.Number));
                Assert.That(scalar.Number, Is.EqualTo(1));
            });
        }

        [Test]
        public void CastToBoolRespectsLuaTruthinessRules()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DynValue.Nil.CastToBool(), Is.False);
                Assert.That(DynValue.Void.CastToBool(), Is.False);
                Assert.That(DynValue.False.CastToBool(), Is.False);
                Assert.That(DynValue.NewString("value").CastToBool(), Is.True);
                Assert.That(DynValue.NewNumber(0).CastToBool(), Is.True);
            });
        }

        [Test]
        public void GetLengthSupportsStringsAndTables()
        {
            DynValue @string = DynValue.NewString("abcd");
            Table table = new(null);
            table.Set(1, DynValue.NewNumber(10));
            table.Set(2, DynValue.NewNumber(20));
            DynValue tableValue = DynValue.NewTable(table);

            DynValue stringLength = @string.GetLength();
            DynValue tableLength = tableValue.GetLength();

            Assert.Multiple(() =>
            {
                Assert.That(stringLength.Number, Is.EqualTo(4));
                Assert.That(tableLength.Number, Is.EqualTo(2));
            });
        }

        [Test]
        public void GetLengthThrowsWhenTypeHasNoLength()
        {
            DynValue number = DynValue.NewNumber(5);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                number.GetLength()
            );

            Assert.That(exception.Message, Does.Contain("Can't get length"));
        }

        [Test]
        public void AssignCopiesWritableValuesAndResetsHashcode()
        {
            DynValue destination = DynValue.NewNumber(1);
            _ = destination.GetHashCode(); // populate cached hash

            DynValue source = DynValue.NewString("hello");
            destination.Assign(source);

            Assert.Multiple(() =>
            {
                Assert.That(destination.Type, Is.EqualTo(DataType.String));
                Assert.That(destination.String, Is.EqualTo("hello"));
                Assert.That(destination.GetHashCode(), Is.EqualTo(source.GetHashCode()));
            });
        }

        [Test]
        public void AssignThrowsWhenValueIsReadOnly()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                DynValue.True.Assign(DynValue.False)
            );

            Assert.That(exception.Message, Does.Contain("Assigning on r-value"));
        }

        [Test]
        public void CheckTypeAutoConvertsNumbersToStrings()
        {
            DynValue number = DynValue.NewNumber(12.5);

            DynValue converted = number.CheckType(
                "func",
                DataType.String,
                argNum: 0,
                flags: TypeValidationFlags.AutoConvert
            );

            Assert.Multiple(() =>
            {
                Assert.That(converted.Type, Is.EqualTo(DataType.String));
                Assert.That(converted.String, Is.EqualTo("12.5"));
            });
        }

        [Test]
        public void CheckTypeThrowsWhenVoidAndValueRequired()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                DynValue.Void.CheckType("func", DataType.Number, argNum: 2)
            );

            Assert.That(exception.Message, Does.Contain("got no value"));
        }

        [Test]
        public void CastToNumberParsesInvariantStrings()
        {
            DynValue numericString = DynValue.NewString("12.75");
            double? result = numericString.CastToNumber();

            Assert.That(result, Is.EqualTo(12.75));
        }

        [Test]
        public void CastToNumberReturnsNullForNonNumericStrings()
        {
            Assert.That(DynValue.NewString("not-a-number").CastToNumber(), Is.Null);
        }

        [Test]
        public void CastToStringConvertsNumbers()
        {
            DynValue number = DynValue.NewNumber(5.5);

            Assert.That(number.CastToString(), Is.EqualTo("5.5"));
        }

        [Test]
        public void CheckTypeAllowsNilWhenFlagSet()
        {
            DynValue result = DynValue.Nil.CheckType(
                "func",
                DataType.Table,
                flags: TypeValidationFlags.AllowNil
            );

            Assert.That(result, Is.SameAs(DynValue.Nil));
        }

        [Test]
        public void CheckUserDataTypeReturnsManagedInstance()
        {
            DynValue userData = UserData.Create(new SampleUserData("ud"));

            SampleUserData result = userData.CheckUserDataType<SampleUserData>("func");

            Assert.That(result.Name, Is.EqualTo("ud"));
        }

        [Test]
        public void CheckUserDataTypeThrowsWhenTypeMismatch()
        {
            DynValue userData = UserData.Create(new SampleUserData("ud"));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                userData.CheckUserDataType<string>("func")
            );

            Assert.That(exception.Message, Does.Contain("userdata"));
        }

        [Test]
        public void ToPrintStringReflectsUserDataDescriptor()
        {
            DynValue userData = UserData.Create(new SampleUserData("Printable"));

            Assert.That(userData.ToPrintString(), Is.EqualTo("Printable"));
        }

        [Test]
        public void AsReadOnlyReturnsSameInstanceWhenAlreadyReadOnly()
        {
            Assert.That(DynValue.True.AsReadOnly(), Is.SameAs(DynValue.True));
        }

        [Test]
        public void GetHashCodeCachesPerInstance()
        {
            DynValue str = DynValue.NewString("hash-me");

            int first = str.GetHashCode();
            int second = str.GetHashCode();

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ToDebugPrintStringFlattensTuples()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewString("x"), DynValue.NewNumber(4));

            Assert.That(tuple.ToDebugPrintString(), Is.EqualTo("x\t4"));
        }

        [Test]
        public void IsNilOrNanDetectsNaN()
        {
            DynValue value = DynValue.NewNumber(double.NaN);
            Assert.That(value.IsNilOrNan(), Is.True);
        }

        [Test]
        public void GetAsPrivateResourceReturnsUnderlyingResource()
        {
            Script script = new();
            Table table = new(script);
            DynValue tableValue = DynValue.NewTable(table);

            Assert.That(tableValue.GetAsPrivateResource(), Is.SameAs(table));
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
    }
}
