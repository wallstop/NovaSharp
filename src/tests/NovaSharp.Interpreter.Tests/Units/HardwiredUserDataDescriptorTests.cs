namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwiredUserDataDescriptorTests
    {
        static HardwiredUserDataDescriptorTests()
        {
            _ = new SampleUserData();
        }

        [Test]
        public void FriendlyNamePrefixedForHardwiredDescriptors()
        {
            SampleHardwiredDescriptor descriptor = new SampleHardwiredDescriptor();

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.FriendlyName, Is.EqualTo("::hardwired::SampleUserData"));
                Assert.That(descriptor.Type, Is.EqualTo(typeof(SampleUserData)));
                Assert.That(descriptor.Name, Is.EqualTo(typeof(SampleUserData).FullName));
            });
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
