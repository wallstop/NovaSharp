namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Options;

    [ScriptGlobalOptionsIsolation]
    public sealed class PropertyTableAssignerTUnitTests
    {
        static PropertyTableAssignerTUnitTests()
        {
            _ = new DuplicateProperties();
            _ = new AddressInfo();
        }

        private Script _script;

        [global::TUnit.Core.Before(global::TUnit.Core.HookType.Test)]
        public void SetUp()
        {
            _script = new Script();
        }

        [global::TUnit.Core.Test]
        public async Task AssignObjectSetsAttributedProperties()
        {
            Table data = new(_script);
            data.Set("name", DynValue.NewString("Nova"));
            data.Set("count", DynValue.NewNumber(5));

            BasicSample target = new();
            PropertyTableAssigner<BasicSample> assigner = new();

            assigner.AssignObject(target, data);

            await Assert.That(target.Name).IsEqualTo("Nova").ConfigureAwait(false);
            await Assert.That(target.Count).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AssignObjectUsesSubassignerForNestedTables()
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

            await Assert.That(target.Address).IsNotNull().ConfigureAwait(false);
            await Assert.That(target.Address!.Street).IsEqualTo("Main").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void AssignObjectThrowsForUnexpectedProperty()
        {
            Table data = new(_script);
            data.Set("unknown", DynValue.NewNumber(1));

            PropertyTableAssigner<BasicSample> assigner = new();

            Assert.Throws<ScriptRuntimeException>(() =>
                assigner.AssignObject(new BasicSample(), data)
            );
        }

        [global::TUnit.Core.Test]
        public void AddExpectedMissingPropertySuppressesError()
        {
            Table data = new(_script);
            data.Set("unknown", DynValue.NewNumber(1));

            PropertyTableAssigner<BasicSample> assigner = new();
            assigner.AddExpectedMissingProperty("unknown");

            BasicSample target = new();
            assigner.AssignObject(target, data);
        }

        [global::TUnit.Core.Test]
        public void AssignObjectRequiresCompatibleInstance()
        {
            PropertyTableAssigner<BasicSample> assigner = new();
            Table data = new(_script);

            Assert.Throws<ArgumentNullException>(() => assigner.AssignObject(null, data));

            PropertyTableAssigner nonGeneric = new(typeof(BasicSample));
            Assert.Throws<ArgumentException>(() =>
                nonGeneric.AssignObject(new IncompatibleSample(), data)
            );
        }

        [global::TUnit.Core.Test]
        public void AssignObjectRequiresStringKey()
        {
            Table data = new(_script);
            data.Set(1, DynValue.NewNumber(5));

            PropertyTableAssigner<BasicSample> assigner = new();

            Assert.Throws<ScriptRuntimeException>(() =>
                assigner.AssignObject(new BasicSample(), data)
            );
        }

        [global::TUnit.Core.Test]
        public void SetSubassignerForTypeRejectsInvalidTypes()
        {
            PropertyTableAssigner<ParentWithAddress> assigner = new();
            Assert.Throws<ArgumentException>(() =>
                assigner.SetSubassignerForType(typeof(int), null)
            );
        }

        [global::TUnit.Core.Test]
        public async Task FuzzyMatchingAllowsUnderscoreKeys()
        {
            using IDisposable globalScope = Script.BeginGlobalOptionsScope();
            Script.GlobalOptions.FuzzySymbolMatching =
                FuzzySymbolMatchingBehavior.Camelify | FuzzySymbolMatchingBehavior.PascalCase;

            Table data = new(_script);
            data.Set("first_name", DynValue.NewString("Nova"));

            FuzzySample target = new();
            PropertyTableAssigner<FuzzySample> assigner = new();
            assigner.AssignObject(target, data);

            await Assert.That(target.FirstName).IsEqualTo("Nova").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenericAssignerReturnsUnderlyingAssigner()
        {
            Table data = new(_script);
            data.Set("name", DynValue.NewString("Nova"));

            BasicSample target = new();
            PropertyTableAssigner<BasicSample> generic = new();
            PropertyTableAssigner typeUnsafe = generic.TypeUnsafeAssigner;

            typeUnsafe.AssignObject(target, data);

            await Assert.That(target.Name).IsEqualTo("Nova").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AssignObjectUncheckedInvokesGenericAssigner()
        {
            Table data = new(_script);
            data.Set("name", DynValue.NewString("Nova"));

            BasicSample target = new();
            IPropertyTableAssigner assigner = new PropertyTableAssigner<BasicSample>();

            assigner.AssignObjectUnchecked(target, data);

            await Assert.That(target.Name).IsEqualTo("Nova").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void ConstructorAllowsExpectedMissingPropertiesParameter()
        {
            Table data = new(_script);
            data.Set("ignored", DynValue.NewNumber(5));

            PropertyTableAssigner<BasicSample> assigner = new("ignored");

            assigner.AssignObject(new BasicSample(), data);
        }

        [global::TUnit.Core.Test]
        public void ConstructorRejectsValueTypesAndDuplicateNames()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                PropertyTableAssigner assigner = new(typeof(int));
                _ = assigner.GetType();
            });
            Assert.Throws<ArgumentException>(() =>
            {
                PropertyTableAssigner<DuplicateProperties> assigner = new();
                _ = assigner.GetType();
            });
        }

        [global::TUnit.Core.Test]
        public void RemovingSubassignerFallsBackToClrConversion()
        {
            Table address = new(_script);
            address.Set("street", DynValue.NewString("Second"));

            Table data = new(_script);
            data.Set("address", DynValue.NewTable(address));

            PropertyTableAssigner<ParentWithAddress> assigner = new();
            assigner.SetSubassigner(new PropertyTableAssigner<AddressInfo>());
            assigner.AssignObject(new ParentWithAddress(), data);

            assigner.SetSubassigner<AddressInfo>(null);

            Assert.Throws<ScriptRuntimeException>(() =>
                assigner.AssignObject(new ParentWithAddress(), data)
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
