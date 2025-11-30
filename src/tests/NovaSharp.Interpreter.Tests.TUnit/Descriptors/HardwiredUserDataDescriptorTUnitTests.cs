#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

    public sealed class HardwiredUserDataDescriptorTUnitTests
    {
        static HardwiredUserDataDescriptorTUnitTests()
        {
            _ = new SampleUserData();
        }

        [global::TUnit.Core.Test]
        public async Task FriendlyNamePrefixedForHardwiredDescriptors()
        {
            SampleHardwiredDescriptor descriptor = new();

            await Assert.That(descriptor.FriendlyName).IsEqualTo("::hardwired::SampleUserData");
            await Assert.That(descriptor.Type).IsEqualTo(typeof(SampleUserData));
            await Assert.That(descriptor.Name).IsEqualTo(typeof(SampleUserData).FullName);
        }

        private sealed class SampleUserData
        {
            public int Value { get; set; }
        }

        private sealed class SampleHardwiredDescriptor : HardwiredUserDataDescriptor
        {
            public SampleHardwiredDescriptor()
                : base(typeof(SampleUserData)) { }
        }
    }
}
#pragma warning restore CA2007
