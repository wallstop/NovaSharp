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

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(
                () => number.GetLength()
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
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(
                () => DynValue.True.Assign(DynValue.False)
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
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(
                () => DynValue.Void.CheckType("func", DataType.Number, argNum: 2)
            );

            Assert.That(exception.Message, Does.Contain("got no value"));
        }
    }
}
