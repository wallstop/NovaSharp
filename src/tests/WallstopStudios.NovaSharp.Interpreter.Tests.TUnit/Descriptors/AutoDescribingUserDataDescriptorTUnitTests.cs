namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors;

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

            LuaValue value = descriptor
                .Index(script, userdata, LuaValue.NewString("key"), true)
                .Value;
            bool foundVoid = descriptor.TryIndex(
                script,
                userdata,
                LuaValue.NewString("void"),
                true,
                out LuaValue explicitVoid
            );

            await Assert.That(userdata.IndexInvocations).IsEqualTo(2);
            await Assert.That(value.Type).IsEqualTo(DataType.String);
            await Assert.That(value.String).IsEqualTo("indexed:key");
            await Assert.That(foundVoid).IsTrue();
            await Assert.That(explicitVoid.IsVoid()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SetIndexReturnsFalseWhenNotUserDataType()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");
            Script script = new();

            bool result = descriptor.SetIndex(
                script,
                new object(),
                LuaValue.NewString("key"),
                LuaValue.NewNumber(42),
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
                LuaValue.NewString("key"),
                LuaValue.NewNumber(123),
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

            LuaValue meta = descriptor.MetaIndex(script, userdata, "__call").Value;

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

            LuaValue? value = descriptor.Index(
                script,
                new object(),
                LuaValue.NewString("key"),
                true
            );
            bool found = descriptor.TryIndex(
                script,
                new object(),
                LuaValue.NewString("key"),
                true,
                out LuaValue missing
            );

            await Assert.That(value).IsNull();
            await Assert.That(found).IsFalse();
            await Assert.That(missing.IsNil).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task MetaIndexReturnsNullWhenObjIsNotUserDataType()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");
            Script script = new();

            LuaValue? value = descriptor.MetaIndex(script, new object(), "__call");

            await Assert.That(value).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task IsTypeCompatibleUsesFrameworkSemantics()
        {
            AutoDescribingUserDataDescriptor descriptor = new(typeof(SampleUserData), "Sample");

            await Assert.That(descriptor.IsTypeCompatible(typeof(string), "value")).IsTrue();
            await Assert.That(descriptor.IsTypeCompatible(typeof(string), 5)).IsFalse();
        }

        private sealed class SampleUserData : IUserDataTypeTryAccess
        {
            internal int IndexInvocations { get; private set; }

            internal int MetaIndexInvocations { get; private set; }

            internal LuaValue LastSetIndex { get; private set; } = LuaValue.Nil;

            internal LuaValue LastSetValue { get; private set; } = LuaValue.Nil;

            public bool TryIndex(
                Script script,
                LuaValue index,
                bool isDirectIndexing,
                out LuaValue value
            )
            {
                IndexInvocations++;
                value =
                    index.String == "void"
                        ? LuaValue.Void
                        : LuaValue.NewString($"indexed:{index.String}");
                return true;
            }

            public bool SetIndex(
                Script script,
                LuaValue index,
                LuaValue value,
                bool isDirectIndexing
            )
            {
                LastSetIndex = index;
                LastSetValue = value;
                return true;
            }

            public bool TryMetaIndex(Script script, string metaname, out LuaValue value)
            {
                MetaIndexInvocations++;
                value = LuaValue.NewCallback((_, _) => LuaValue.NewString($"meta:{metaname}"));
                return true;
            }

            public override string ToString()
            {
                return nameof(SampleUserData);
            }
        }
    }
}
