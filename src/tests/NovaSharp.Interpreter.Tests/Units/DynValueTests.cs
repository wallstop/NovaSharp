namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
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
        public void NewTupleNestedThrowsWhenValuesNull()
        {
            Assert.That(
                () => DynValue.NewTupleNested((DynValue[])null),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("values")
            );
        }

        [Test]
        public void NewTupleNestedReturnsSingleValueUnchanged()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewString("value"));

            DynValue nested = DynValue.NewTupleNested(tuple);

            Assert.That(nested, Is.SameAs(tuple));
        }

        [Test]
        public void NewTupleNestedWithoutTuplesCreatesRegularTuple()
        {
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewString("two");

            DynValue result = DynValue.NewTupleNested(first, second);

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple, Has.Length.EqualTo(2));
                Assert.That(result.Tuple[0], Is.SameAs(first));
                Assert.That(result.Tuple[1], Is.SameAs(second));
            });
        }

        [Test]
        public void NewTableFromArrayInitializesEntriesAndOwner()
        {
            Script script = new();
            DynValue[] values = new[] { DynValue.NewNumber(7), DynValue.NewString("value") };

            DynValue tableValue = DynValue.NewTable(script, values);

            Assert.Multiple(() =>
            {
                Assert.That(tableValue.Table.OwnerScript, Is.SameAs(script));
                Assert.That(tableValue.Table.Length, Is.EqualTo(2));
                Assert.That(tableValue.Table.Get(1).Number, Is.EqualTo(7));
                Assert.That(tableValue.Table.Get(2).String, Is.EqualTo("value"));
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
                flags: TypeValidationOptions.AutoConvert
            );

            Assert.Multiple(() =>
            {
                Assert.That(converted.Type, Is.EqualTo(DataType.String));
                Assert.That(converted.String, Is.EqualTo("12.5"));
            });
        }

        [Test]
        public void CheckTypeThrowsWhenConversionNotAllowed()
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

            Assert.That(exception.Message, Does.Contain("bad argument #1"));
        }

        [Test]
        public void GetLengthThrowsOnUnsupportedTypes()
        {
            Script script = new();
            CallbackFunction callback = new CallbackFunction((_, _) => DynValue.True);
            DynValue function = DynValue.NewCallback(callback);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                function.GetLength()
            );

            Assert.That(exception.Message, Does.Contain("Can't get length"));
        }

        [Test]
        public void AssignCopiesObjectReferencesForTables()
        {
            Script script = new();
            Table table = new(script);
            DynValue destination = DynValue.NewNil();

            destination.Assign(DynValue.NewTable(table));

            Assert.Multiple(() =>
            {
                Assert.That(destination.Type, Is.EqualTo(DataType.Table));
                Assert.That(destination.Table, Is.SameAs(table));
            });
        }

        [Test]
        public void AssignPreservesReadOnlyForDestination()
        {
            DynValue destination = DynValue.NewNumber(1).AsReadOnly();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                destination.Assign(DynValue.NewNumber(2))
            );

            Assert.That(exception.Message, Does.Contain("Assigning on r-value"));
        }

        [Test]
        public void AssignNumberThrowsWhenDestinationReadOnly()
        {
            DynValue destination = DynValue.NewNumber(1).AsReadOnly();

            Assert.Throws<InternalErrorException>(() => destination.AssignNumber(2));
        }

        [Test]
        public void AssignNumberThrowsWhenDestinationIsNotNumeric()
        {
            DynValue destination = DynValue.NewString("value");

            Assert.Throws<InternalErrorException>(() => destination.AssignNumber(2));
        }

        [Test]
        public void GetAsPrivateResourceReturnsNullWhenNotPrivate()
        {
            DynValue number = DynValue.NewNumber(5);

            Assert.That(number.ScriptPrivateResource, Is.Null);
        }

        [Test]
        public void GetTypeConvertsValueToRequestedType()
        {
            DynValue number = DynValue.NewNumber(7);

            double converted = number.ToObject<double>();

            Assert.That(converted, Is.EqualTo(7d));
        }

        [Test]
        public void TypeChecksThrowWhenTypeMissing()
        {
            DynValue nil = DynValue.Nil;

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                nil.CheckType("func", DataType.Number, argNum: 1)
            );

            Assert.That(exception.Message, Does.Contain("bad argument #2"));
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
                flags: TypeValidationOptions.AllowNil
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
        public void CheckUserDataTypeAllowsNilWhenFlagged()
        {
            SampleUserData result = DynValue.Nil.CheckUserDataType<SampleUserData>(
                "func",
                flags: TypeValidationOptions.AllowNil
            );

            Assert.That(result, Is.Null);
        }

        [Test]
        public void NewStringFromStringBuilderCopiesSnapshot()
        {
            StringBuilder builder = new("seed");
            DynValue value = DynValue.NewString(builder);
            builder.Append("mutated");

            Assert.That(value.String, Is.EqualTo("seed"));
        }

        [Test]
        public void NewStringFromStringBuilderThrowsWhenBuilderIsNull()
        {
            Assert.That(
                () => DynValue.NewString((StringBuilder)null),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("sb")
            );
        }

        [Test]
        public void NewStringFormatThrowsWhenFormatIsNull()
        {
            Assert.That(
                () => DynValue.NewString(null, "value"),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("format")
            );
        }

        [Test]
        public void NewStringFormatAppliesArguments()
        {
            DynValue value = DynValue.NewString("value {0} {1}", 5, "x");

            Assert.That(value.String, Is.EqualTo("value 5 x"));
        }

        [Test]
        public void NewStringFormatReturnsLiteralWhenArgsNull()
        {
            DynValue value = DynValue.NewString("literal", (object[])null);

            Assert.That(value.String, Is.EqualTo("literal"));
        }

        [Test]
        public void CloneReflectsReadOnlyPreference()
        {
            DynValue number = DynValue.NewNumber(7);
            DynValue readOnly = number.Clone(true);
            DynValue writable = readOnly.Clone(false);

            Assert.Multiple(() =>
            {
                Assert.That(readOnly.ReadOnly, Is.True);
                Assert.That(writable.ReadOnly, Is.False);
            });
        }

        [Test]
        public void CloneAsWritableProducesEditableCopy()
        {
            DynValue readOnly = DynValue.NewString("locked").AsReadOnly();
            DynValue clone = readOnly.CloneAsWritable();

            clone.Assign(DynValue.NewString("unlocked"));

            Assert.Multiple(() =>
            {
                Assert.That(clone.String, Is.EqualTo("unlocked"));
                Assert.That(readOnly.String, Is.EqualTo("locked"));
            });
        }

        [Test]
        public void AssignUpdatesTargetAndResetsHashCode()
        {
            DynValue target = DynValue.NewNumber(1);
            int oldHash = target.GetHashCode();

            target.Assign(DynValue.NewString("assigned"));

            Assert.Multiple(() =>
            {
                Assert.That(target.Type, Is.EqualTo(DataType.String));
                Assert.That(target.String, Is.EqualTo("assigned"));
                Assert.That(target.GetHashCode(), Is.Not.EqualTo(oldHash));
            });
        }

        [Test]
        public void AssignThrowsWhenTargetIsReadOnly()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                DynValue.True.Assign(DynValue.NewNumber(2))
            );

            Assert.That(exception.Message, Does.Contain("Assigning on r-value"));
        }

        [Test]
        public void NewCoroutineWrapsCoroutineHandles()
        {
            Script script = new();
            DynValue function = script.Call(
                script.LoadString("return function(x) coroutine.yield(x); return x end")
            );
            DynValue coroutineValue = script.CreateCoroutine(function);
            DynValue wrapped = DynValue.NewCoroutine(coroutineValue.Coroutine);

            Assert.Multiple(() =>
            {
                Assert.That(wrapped.Type, Is.EqualTo(DataType.Thread));
                Assert.That(wrapped.Coroutine, Is.SameAs(coroutineValue.Coroutine));
                Assert.That(wrapped.ToString(), Does.Contain("Coroutine"));
            });
        }

        [Test]
        public void ToStringFormatsClrFunctions()
        {
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil, "named");
            Assert.That(callback.ToString(), Is.EqualTo("(Function CLR)"));
        }

        [Test]
        public void ToStringCoversLuaTypeRepresentations()
        {
            Script script = new();
            DynValue chunk = script.LoadString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(chunk);
            DynValue tableValue = DynValue.NewTable(new Table(script));
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewString("two"));
            DynValue userData = UserData.Create(new SampleUserData("ignored"));
            DynValue yield = DynValue.NewYieldReq(Array.Empty<DynValue>());

            Assert.Multiple(() =>
            {
                Assert.That(DynValue.Void.ToString(), Is.EqualTo("void"));
                Assert.That(chunk.ToString(), Does.StartWith("(Function "));
                Assert.That(tableValue.ToString(), Is.EqualTo("(Table)"));
                Assert.That(tuple.ToString(), Is.EqualTo("1, \"two\""));
                Assert.That(userData.ToString(), Is.EqualTo("(UserData)"));
                Assert.That(coroutine.ToString(), Does.StartWith("(Coroutine "));
                Assert.That(yield.ToString(), Is.EqualTo("(???)"));
            });
        }

        [Test]
        public void CheckTypeAutoConvertsAcrossCoreTypes()
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

            Assert.Multiple(() =>
            {
                Assert.That(boolValue.Boolean, Is.True);
                Assert.That(numberValue.Number, Is.EqualTo(42));
                Assert.That(stringValue.String, Is.EqualTo("3.5"));
            });
        }

        [Test]
        public void CheckTypeComplainsWhenVoidHasNoValue()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                DynValue.Void.CheckType("func", DataType.String)
            );

            Assert.That(exception.Message, Does.Contain("no value"));
        }

        [Test]
        public void CheckTypeAutoConvertFallbacksReturnOriginalWhenConversionFails()
        {
            DynValue original = DynValue.NewString("not-number");
            DynValue result = original.CheckType(
                "func",
                DataType.String,
                flags: TypeValidationOptions.AutoConvert
            );

            Assert.That(result, Is.SameAs(original));
        }

        [Test]
        public void CheckUserDataTypeReturnsDefaultWhenNilAllowed()
        {
            SampleUserData result = DynValue.Nil.CheckUserDataType<SampleUserData>(
                "func",
                flags: TypeValidationOptions.AllowNil
            );

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ToPrintStringReflectsUserDataDescriptor()
        {
            DynValue userData = UserData.Create(new SampleUserData("Printable"));

            Assert.That(userData.ToPrintString(), Is.EqualTo("Printable"));
        }

        [Test]
        public void ToPrintStringFormatsCompositeValues()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewString("a"), DynValue.NewNumber(5));
            DynValue tail = DynValue.NewTailCallReq(
                DynValue.NewCallback((_, _) => DynValue.Nil),
                DynValue.NewNumber(1)
            );
            DynValue yield = DynValue.NewYieldReq(Array.Empty<DynValue>());

            Assert.Multiple(() =>
            {
                Assert.That(tuple.ToPrintString(), Is.EqualTo("a\t5"));
                Assert.That(tail.ToPrintString(), Is.EqualTo("(TailCallRequest -- INTERNAL!)"));
                Assert.That(yield.ToPrintString(), Is.EqualTo("(YieldRequest -- INTERNAL!)"));
            });
        }

        [Test]
        public void ToPrintStringFallsBackToRefIdForTablesAndUserData()
        {
            Script script = new();
            DynValue tableValue = DynValue.NewTable(new Table(script));
            DynValue userData = UserData.Create(new object(), new NullStringDescriptor());

            Assert.Multiple(() =>
            {
                Assert.That(tableValue.ToPrintString(), Does.StartWith("table: "));
                Assert.That(userData.ToPrintString(), Does.StartWith("userdata: "));
            });
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
        public void GetHashCodeHandlesNilAndTupleCases()
        {
            int nilHash = DynValue.Nil.GetHashCode();
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2));
            int tupleHash = tuple.GetHashCode();

            Assert.Multiple(() =>
            {
                Assert.That(nilHash, Is.EqualTo(DynValue.Nil.GetHashCode()));
                Assert.That(tupleHash, Is.EqualTo(tuple.GetHashCode()));
            });
        }

        [Test]
        public void EqualsHandlesNonDynValuesTuplesUserDataAndYieldRequests()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2));
            DynValue alias = DynValue.NewNil();
            alias.Assign(tuple);
            DynValue tupleCopy = DynValue.NewTuple(DynValue.NewNumber(1), DynValue.NewNumber(2));
            DynValue nullUserData = DynValue.NewUserData(null);
            DynValue userData = UserData.Create(new SampleUserData("value"));
            DynValue forcedYield = DynValue.NewForcedYieldReq();

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Equals("value"), Is.False);
                Assert.That(tuple.Equals(alias), Is.True);
                Assert.That(tuple.Equals(tupleCopy), Is.False);
                Assert.That(nullUserData.Equals(userData), Is.False);
                Assert.That(forcedYield.Equals(forcedYield), Is.True);
            });
        }

        [Test]
        public void ToDebugPrintStringFlattensTuples()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewString("x"), DynValue.NewNumber(4));

            Assert.That(tuple.ToDebugPrintString(), Is.EqualTo("x\t4"));
        }

        [Test]
        public void ToDebugPrintStringDisplaysTailYieldAndScalars()
        {
            DynValue tail = DynValue.NewTailCallReq(
                DynValue.NewCallback((_, _) => DynValue.Nil),
                DynValue.NewNumber(9)
            );
            DynValue yield = DynValue.NewYieldReq(Array.Empty<DynValue>());

            Assert.Multiple(() =>
            {
                Assert.That(tail.ToDebugPrintString(), Is.EqualTo("(TailCallRequest)"));
                Assert.That(yield.ToDebugPrintString(), Is.EqualTo("(YieldRequest)"));
                Assert.That(
                    DynValue.True.ToDebugPrintString(),
                    Is.EqualTo(DynValue.True.ToString())
                );
            });
        }

        [Test]
        public void IsNilOrNanDetectsNaN()
        {
            DynValue value = DynValue.NewNumber(double.NaN);
            Assert.That(value.IsNilOrNan(), Is.True);
        }

        [Test]
        public void IsNotVoidDistinguishesVoidValues()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DynValue.Void.IsNotVoid(), Is.False);
                Assert.That(DynValue.NewNumber(1).IsNotVoid(), Is.True);
            });
        }

        [Test]
        public void GetAsPrivateResourceReturnsUnderlyingResource()
        {
            Script script = new();
            Table table = new(script);
            DynValue tableValue = DynValue.NewTable(table);

            Assert.That(tableValue.ScriptPrivateResource, Is.SameAs(table));
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
