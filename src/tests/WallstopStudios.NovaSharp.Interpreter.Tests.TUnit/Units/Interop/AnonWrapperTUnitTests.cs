namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Interop.PredefinedUserData;

    public sealed class AnonWrapperTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ParameterlessConstructorLeavesValueUnset()
        {
            AnonWrapper<string> wrapper = new();
            await Assert.That(wrapper.Value is null).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ValueConstructorStoresPayload()
        {
            AnonWrapper<int> wrapper = new(42);
            await Assert.That(wrapper.Value).IsEqualTo(42).ConfigureAwait(false);

            wrapper.Value = 7;
            await Assert.That(wrapper.Value).IsEqualTo(7).ConfigureAwait(false);
        }
    }
}
