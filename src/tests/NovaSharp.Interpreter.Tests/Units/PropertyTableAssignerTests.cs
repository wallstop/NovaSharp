namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Options;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PropertyTableAssignerTests
    {
        static PropertyTableAssignerTests()
        {
            _ = new DuplicateProperties();
            _ = new AddressInfo();
        }

        private Script _script = null!;

        [SetUp]
        public void SetUp()
        {
            _script = new Script();
        }

        [Test]
        public void AssignObjectSetsAttributedProperties()
        {
            Table data = new(_script);
            data.Set("name", DynValue.NewString("Nova"));
            data.Set("count", DynValue.NewNumber(5));

            BasicSample target = new();
            PropertyTableAssigner<BasicSample> assigner = new();

            assigner.AssignObject(target, data);

            Assert.Multiple(() =>
            {
                Assert.That(target.Name, Is.EqualTo("Nova"));
                Assert.That(target.Count, Is.EqualTo(5));
            });
        }

        [Test]
        public void AssignObjectUsesSubassignerForNestedTables()
        {
            Table addressTable = new(_script);
            addressTable.Set("street", DynValue.NewString("Main"));

            Table data = new(_script);
            data.Set("address", DynValue.NewTable(addressTable));

            ParentWithAddress target = new();
            PropertyTableAssigner<ParentWithAddress> assigner = new();
            assigner.SetSubassignerForType(
                typeof(AddressInfo),
                new PropertyTableAssigner<AddressInfo>()
            );

            assigner.AssignObject(target, data);

            Assert.Multiple(() =>
            {
                Assert.That(target.Address, Is.Not.Null);
                Assert.That(target.Address!.Street, Is.EqualTo("Main"));
            });
        }

        [Test]
        public void AssignObjectThrowsForUnexpectedProperty()
        {
            Table data = new(_script);
            data.Set("unknown", DynValue.NewNumber(1));

            PropertyTableAssigner<BasicSample> assigner = new();

            Assert.That(
                () => assigner.AssignObject(new BasicSample(), data),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void AddExpectedMissingPropertySuppressesError()
        {
            Table data = new(_script);
            data.Set("unknown", DynValue.NewNumber(1));

            PropertyTableAssigner<BasicSample> assigner = new();
            assigner.AddExpectedMissingProperty("unknown");

            BasicSample target = new();
            Assert.That(() => assigner.AssignObject(target, data), Throws.Nothing);
        }

        [Test]
        public void AssignObjectRequiresCompatibleInstance()
        {
            PropertyTableAssigner<BasicSample> assigner = new();
            Table data = new(_script);

            Assert.That(() => assigner.AssignObject(null!, data), Throws.ArgumentNullException);

            PropertyTableAssigner nonGeneric = new(typeof(BasicSample));
            Assert.That(
                () => nonGeneric.AssignObject(new IncompatibleSample(), data),
                Throws.TypeOf<ArgumentException>()
            );
        }

        [Test]
        public void AssignObjectRequiresStringKey()
        {
            Table data = new(_script);
            data.Set(1, DynValue.NewNumber(5));

            PropertyTableAssigner<BasicSample> assigner = new();

            Assert.That(
                () => assigner.AssignObject(new BasicSample(), data),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void SetSubassignerForTypeRejectsInvalidTypes()
        {
            PropertyTableAssigner<ParentWithAddress> assigner = new();
            Assert.That(
                () => assigner.SetSubassignerForType(typeof(int), null),
                Throws.TypeOf<ArgumentException>()
            );
        }

        [Test]
        public void FuzzyMatchingAllowsUnderscoreKeys()
        {
            FuzzySymbolMatchingBehavior previous = Script.GlobalOptions.FuzzySymbolMatching;
            Script.GlobalOptions.FuzzySymbolMatching =
                FuzzySymbolMatchingBehavior.Camelify | FuzzySymbolMatchingBehavior.PascalCase;

            try
            {
                Table data = new(_script);
                data.Set("first_name", DynValue.NewString("Nova"));

                FuzzySample target = new();
                PropertyTableAssigner<FuzzySample> assigner = new();
                assigner.AssignObject(target, data);

                Assert.That(target.FirstName, Is.EqualTo("Nova"));
            }
            finally
            {
                Script.GlobalOptions.FuzzySymbolMatching = previous;
            }
        }

        [Test]
        public void GenericAssignerReturnsUnderlyingAssigner()
        {
            Table data = new(_script);
            data.Set("name", DynValue.NewString("Nova"));

            BasicSample target = new();
            PropertyTableAssigner<BasicSample> generic = new();
            PropertyTableAssigner typeUnsafe = generic.TypeUnsafeAssigner;

            typeUnsafe.AssignObject(target, data);

            Assert.That(target.Name, Is.EqualTo("Nova"));
        }

        [Test]
        public void AssignObjectUncheckedInvokesGenericAssigner()
        {
            Table data = new(_script);
            data.Set("name", DynValue.NewString("Nova"));

            BasicSample target = new();
            IPropertyTableAssigner assigner = new PropertyTableAssigner<BasicSample>();

            assigner.AssignObjectUnchecked(target, data);

            Assert.That(target.Name, Is.EqualTo("Nova"));
        }

        [Test]
        public void ConstructorAllowsExpectedMissingPropertiesParameter()
        {
            Table data = new(_script);
            data.Set("ignored", DynValue.NewNumber(5));

            PropertyTableAssigner<BasicSample> assigner = new("ignored");

            Assert.That(() => assigner.AssignObject(new BasicSample(), data), Throws.Nothing);
        }

        [Test]
        public void ConstructorRejectsValueTypesAndDuplicateNames()
        {
            Assert.That(() => new PropertyTableAssigner(typeof(int)), Throws.ArgumentException);

            Assert.That(
                () => new PropertyTableAssigner<DuplicateProperties>(),
                Throws.ArgumentException.With.Message.Contains("two definitions")
            );
        }

        [Test]
        public void RemovingSubassignerFallsBackToClrConversion()
        {
            Table address = new(_script);
            address.Set("street", DynValue.NewString("Second"));

            Table data = new(_script);
            data.Set("address", DynValue.NewTable(address));

            PropertyTableAssigner<ParentWithAddress> assigner = new();
            assigner.SetSubassigner(new PropertyTableAssigner<AddressInfo>());
            assigner.AssignObject(new ParentWithAddress(), data); // works with subassigner

            assigner.SetSubassigner<AddressInfo>(null);

            Assert.That(
                () => assigner.AssignObject(new ParentWithAddress(), data),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        private sealed class DuplicateProperties
        {
            [NovaSharpProperty("alias")]
            public string First { get; set; }

            [NovaSharpProperty("alias")]
            public string Second { get; set; }
        }

        private sealed class BasicSample
        {
            [NovaSharpProperty("name")]
            public string Name { get; set; }

            [NovaSharpProperty("count")]
            public int Count { get; set; }
        }

        private sealed class AddressInfo
        {
            [NovaSharpProperty("street")]
            public string Street { get; set; }
        }

        private sealed class ParentWithAddress
        {
            [NovaSharpProperty("address")]
            public AddressInfo Address { get; set; }
        }

        private sealed class FuzzySample
        {
            [NovaSharpProperty]
            public string FirstName { get; set; }
        }

        private sealed class IncompatibleSample { }
    }
}
