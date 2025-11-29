#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Isolation
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Tests;

    [UserDataIsolation]
    public sealed class UserDataIsolationSmokeTUnitTests
    {
        [SuppressMessage(
            "Performance",
            "CA1812:Avoid uninstantiated internal classes",
            Justification = "Instantiated via reflection when registering user data."
        )]
        private sealed class SmokeHost { }

        [global::TUnit.Core.Test]
        public async Task RegistrationDoesNotLeakBetweenTestsFirst()
        {
            await Assert.That(UserData.IsTypeRegistered<SmokeHost>()).IsFalse();
            UserData.RegisterType<SmokeHost>();
        }

        [global::TUnit.Core.Test]
        public async Task RegistrationDoesNotLeakBetweenTestsSecond()
        {
            await Assert.That(UserData.IsTypeRegistered<SmokeHost>()).IsFalse();
        }
    }
}
#pragma warning restore CA2007
