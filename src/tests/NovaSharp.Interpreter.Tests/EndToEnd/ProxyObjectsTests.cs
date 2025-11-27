namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using DataTypes;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NUnit.Framework;

    [TestFixture]
    public class ProxyObjectsTests
    {
        private sealed class Proxy
        {
            [NovaSharpVisible(false)]
            [SuppressMessage(
                "Security",
                "CA5394:Random is an insecure random number generator",
                Justification = "Tests intentionally proxy System.Random instances; security is not in scope."
            )]
            public Random RandomSource { get; }

            [NovaSharpVisible(false)]
            public Proxy(Random r)
            {
                RandomSource = r;
            }

            [SuppressMessage(
                "Design",
                "CA1024:UsePropertiesWhereAppropriate",
                Justification = "Proxy tests validate callable getters exposed to Lua."
            )]
            [SuppressMessage(
                "Security",
                "CA5394:Random is an insecure random number generator",
                Justification = "System.Random is intentional for this proxy scenario; cryptographic strength is not required."
            )]
            public int GetValue()
            {
                return RandomSource != null ? RandomSource.Next(3, 4) : 3;
            }
        }

        [Test]
        public void ProxyTest()
        {
            UserData.RegisterProxyType<Proxy, Random>(r => new Proxy(r));

            Script s = new()
            {
                Globals =
                {
                    ["R"] = CreateNonCryptographicRandom(),
                    ["func"] =
                        (Action<Random>)(
                            r =>
                            {
                                Assert.That(r, Is.Not.Null);
                                Assert.That(r is Random, Is.True);
                            }
                        ),
                },
            };

            s.DoString(
                @"
				x = R.GetValue();
				func(R);
			"
            );

            Assert.That(s.Globals.Get("x").Number, Is.EqualTo(3.0));
        }

        [SuppressMessage(
            "Security",
            "CA5394:Random is an insecure random number generator",
            Justification = "Tests explicitly validate proxying of System.Random instances; cryptographic strength is irrelevant."
        )]
        private static Random CreateNonCryptographicRandom()
        {
            return new Random();
        }
    }
}
