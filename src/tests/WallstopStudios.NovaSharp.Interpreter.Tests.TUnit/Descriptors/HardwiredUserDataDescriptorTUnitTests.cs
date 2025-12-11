namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

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

            await Assert
                .That(descriptor.FriendlyName)
                .IsEqualTo("::hardwired::SampleUserData")
                .ConfigureAwait(false);
            await Assert
                .That(descriptor.Type)
                .IsEqualTo(typeof(SampleUserData))
                .ConfigureAwait(false);
            await Assert
                .That(descriptor.Name)
                .IsEqualTo(typeof(SampleUserData).FullName)
                .ConfigureAwait(false);
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
