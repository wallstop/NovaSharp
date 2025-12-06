namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class ProxyObjectsTUnitTests
    {
        [SuppressMessage(
            "Performance",
            "CA1812:Avoid uninstantiated internal classes",
            Justification = "Proxy surface is instantiated when registering the proxy type."
        )]
        private sealed class Proxy
        {
            [NovaSharpVisible(false)]
            [SuppressMessage(
                "Security",
                "CA5394:Random is an insecure random number generator",
                Justification = "Test deliberately proxies System.Random instances; cryptographic strength is not in scope."
            )]
            public Random RandomSource { get; }

            [NovaSharpVisible(false)]
            public Proxy(Random random)
            {
                RandomSource = random;
            }

            [SuppressMessage(
                "Design",
                "CA1024:UsePropertiesWhereAppropriate",
                Justification = "Proxy scenario relies on method syntax for Lua access."
            )]
            [SuppressMessage(
                "Security",
                "CA5394:Random is an insecure random number generator",
                Justification = "System.Random is intentional for this scenario."
            )]
            public int GetValue()
            {
                return RandomSource != null ? RandomSource.Next(3, 4) : 3;
            }
        }

        [global::TUnit.Core.Test]
        public async Task ProxySurfaceAllowsAccessToRandom()
        {
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<Proxy>(ensureUnregistered: true);

            UserData.RegisterProxyType<Proxy, Random>(r => new Proxy(r));

            bool callbackInvoked = false;
            Random capturedRandom = null;

            Script script = new()
            {
                Globals =
                {
                    ["R"] = CreateNonCryptographicRandom(),
                    ["func"] =
                        (Action<Random>)(
                            target =>
                            {
                                callbackInvoked = true;
                                capturedRandom = target;
                            }
                        ),
                },
            };

            script.DoString(
                @"
                    x = R.GetValue();
                    func(R);
                    "
            );

            await Assert.That(script.Globals.Get("x").Number).IsEqualTo(3.0).ConfigureAwait(false);
            await Assert.That(callbackInvoked).IsTrue().ConfigureAwait(false);
            await Assert.That(capturedRandom).IsNotNull().ConfigureAwait(false);
        }

        [SuppressMessage(
            "Security",
            "CA5394:Random is an insecure random number generator",
            Justification = "Tests intentionally exercise System.Random interop."
        )]
        private static Random CreateNonCryptographicRandom()
        {
            return new Random();
        }
    }
}
