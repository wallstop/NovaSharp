namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class AutoDescribingUserDataDescriptorTests
    {
        [Test]
        public void NameAndTypeReflectConstructor()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Friendly");

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Name, Is.EqualTo("Friendly"));
                Assert.That(descriptor.Type, Is.EqualTo(typeof(SampleUserData)));
            });
        }

        [Test]
        public void IndexDelegatesToUserDataType()
        {
            Script script = new();
            SampleUserData userdata = new();
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");

            DynValue value = descriptor.Index(script, userdata, DynValue.NewString("key"), true);

            Assert.Multiple(() =>
            {
                Assert.That(userdata.IndexInvocations, Is.EqualTo(1));
                Assert.That(value.Type, Is.EqualTo(DataType.String));
                Assert.That(value.String, Is.EqualTo("indexed:key"));
            });
        }

        [Test]
        public void SetIndexReturnsFalseWhenNotUserDataType()
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

            Assert.That(result, Is.False);
        }

        [Test]
        public void SetIndexDelegatesToUserDataType()
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

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(userdata.LastSetIndex.String, Is.EqualTo("key"));
                Assert.That(userdata.LastSetValue.Number, Is.EqualTo(123));
            });
        }

        [Test]
        public void MetaIndexDelegatesToUserDataType()
        {
            Script script = new();
            SampleUserData userdata = new();
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");

            DynValue meta = descriptor.MetaIndex(script, userdata, "__call");

            Assert.Multiple(() =>
            {
                Assert.That(userdata.MetaIndexInvocations, Is.EqualTo(1));
                Assert.That(meta.Type, Is.EqualTo(DataType.ClrFunction));
            });
        }

        [Test]
        public void AsStringReturnsObjectToStringAndNullForNil()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");
            SampleUserData userdata = new();

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.AsString(userdata), Is.EqualTo(nameof(SampleUserData)));
                Assert.That(descriptor.AsString(null), Is.Null);
            });
        }

        [Test]
        public void IsTypeCompatibleUsesFrameworkSemantics()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.IsTypeCompatible(typeof(string), "value"), Is.True);
                Assert.That(descriptor.IsTypeCompatible(typeof(string), 5), Is.False);
            });
        }

        private sealed class SampleUserData : IUserDataType
        {
            public int IndexInvocations { get; private set; }

            public int MetaIndexInvocations { get; private set; }

            public DynValue LastSetIndex { get; private set; } = DynValue.Nil;

            public DynValue LastSetValue { get; private set; } = DynValue.Nil;

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
                return DynValue.NewCallback(
                    (context, args) => DynValue.NewString($"meta:{metaname}")
                );
            }

            public override string ToString()
            {
                return nameof(SampleUserData);
            }
        }
    }
}
