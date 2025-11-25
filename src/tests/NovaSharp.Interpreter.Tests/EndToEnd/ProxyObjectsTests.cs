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
        public class Proxy
        {
            [NovaSharpVisible(false)]
            public Random randomSource;

            [NovaSharpVisible(false)]
            public Proxy(Random r)
            {
                randomSource = r;
            }

            [SuppressMessage(
                "Design",
                "CA1024:UsePropertiesWhereAppropriate",
                Justification = "Proxy tests validate callable getters exposed to Lua."
            )]
            public int GetValue()
            {
                return 3;
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
                    ["R"] = new Random(),
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
    }
}
