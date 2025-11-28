namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;

    public sealed class AutoDescribingUserDataDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task NameAndTypeReflectConstructor()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Friendly");

            await Assert.That(descriptor.Name).IsEqualTo("Friendly");
            await Assert.That(descriptor.Type).IsEqualTo(typeof(SampleUserData));
        }

        [global::TUnit.Core.Test]
        public async Task IndexDelegatesToUserDataType()
        {
            Script script = new();
            SampleUserData userdata = new();
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");

            DynValue value = descriptor.Index(script, userdata, DynValue.NewString("key"), true);

            await Assert.That(userdata.IndexInvocations).IsEqualTo(1);
            await Assert.That(value.Type).IsEqualTo(DataType.String);
            await Assert.That(value.String).IsEqualTo("indexed:key");
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexReturnsFalseWhenNotUserDataType()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");
            Script script = new();

            bool result = descriptor.SetIndex(
                script,
                new object(),
                DynValue.NewString("key"),
                DynValue.NewNumber(42),
                false
            );

            await Assert.That(result).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexDelegatesToUserDataType()
        {
            Script script = new();
            SampleUserData userdata = new();
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");

            bool result = descriptor.SetIndex(
                script,
                userdata,
                DynValue.NewString("key"),
                DynValue.NewNumber(123),
                true
            );

            await Assert.That(result).IsTrue();
            await Assert.That(userdata.LastSetIndex.String).IsEqualTo("key");
            await Assert.That(userdata.LastSetValue.Number).IsEqualTo(123d);
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexDelegatesToUserDataType()
        {
            Script script = new();
            SampleUserData userdata = new();
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");

            DynValue meta = descriptor.MetaIndex(script, userdata, "__call");

            await Assert.That(userdata.MetaIndexInvocations).IsEqualTo(1);
            await Assert.That(meta.Type).IsEqualTo(DataType.ClrFunction);
        }

        [global::TUnit.Core.Test]
        public async Task AsStringReturnsObjectToStringAndNullForNil()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");
            SampleUserData userdata = new();

            await Assert.That(descriptor.AsString(userdata)).IsEqualTo(nameof(SampleUserData));
            await Assert.That(descriptor.AsString(null)).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task IndexReturnsNullWhenObjIsNotUserDataType()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");
            Script script = new();

            DynValue value = descriptor.Index(
                script,
                new object(),
                DynValue.NewString("key"),
                true
            );

            await Assert.That(value).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexReturnsNullWhenObjIsNotUserDataType()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");
            Script script = new();

            DynValue value = descriptor.MetaIndex(script, new object(), "__call");

            await Assert.That(value).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task IsTypeCompatibleUsesFrameworkSemantics()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");

            await Assert.That(descriptor.IsTypeCompatible(typeof(string), "value")).IsTrue();
            await Assert.That(descriptor.IsTypeCompatible(typeof(string), 5)).IsFalse();
        }

        private sealed class SampleUserData : IUserDataType
        {
            internal int IndexInvocations { get; private set; }

            internal int MetaIndexInvocations { get; private set; }

            internal DynValue LastSetIndex { get; private set; } = DynValue.Nil;

            internal DynValue LastSetValue { get; private set; } = DynValue.Nil;

            public DynValue Index(Script script, DynValue index, bool isDirectIndexing)
            {
                IndexInvocations++;
                return DynValue.NewString($"indexed:{index.String}");
            }

            public bool SetIndex(
                Script script,
                DynValue index,
                DynValue value,
                bool isDirectIndexing
            )
            {
                LastSetIndex = index;
                LastSetValue = value;
                return true;
            }

            public DynValue MetaIndex(Script script, string metaname)
            {
                MetaIndexInvocations++;
                return DynValue.NewCallback((_, _) => DynValue.NewString($"meta:{metaname}"));
            }

            public override string ToString()
            {
                return nameof(SampleUserData);
            }
        }
    }
}
