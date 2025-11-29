#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Interop.PredefinedUserData;

    public sealed class AnonWrapperTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ParameterlessConstructorLeavesValueUnset()
        {
            AnonWrapper<string> wrapper = new();
            await Assert.That(wrapper.Value is null).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ValueConstructorStoresPayload()
        {
            AnonWrapper<int> wrapper = new(42);
            await Assert.That(wrapper.Value).IsEqualTo(42);

            wrapper.Value = 7;
            await Assert.That(wrapper.Value).IsEqualTo(7);
        }
    }
}
#pragma warning restore CA2007
