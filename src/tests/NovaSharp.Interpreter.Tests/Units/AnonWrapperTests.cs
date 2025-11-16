namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.Interop.PredefinedUserData;
    using NUnit.Framework;

    [TestFixture]
    public sealed class AnonWrapperTests
    {
        [Test]
        public void ParameterlessConstructorLeavesValueUnset()
        {
            AnonWrapper<string> wrapper = new();
            Assert.That(wrapper.Value, Is.Null);
        }

        [Test]
        public void ValueConstructorStoresPayload()
        {
            AnonWrapper<int> wrapper = new(42);
            Assert.That(wrapper.Value, Is.EqualTo(42));

            wrapper.Value = 7;
            Assert.That(wrapper.Value, Is.EqualTo(7));
        }
    }
}
