using System;
using NovaSharp.Interpreter.Interop;
using NUnit.Framework;

namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    [TestFixture]
    public class ProxyObjectsTests
    {
        public class Proxy
        {
            [NovaSharpVisible(false)]
            public Random random;

            [NovaSharpVisible(false)]
            public Proxy(Random r)
            {
                random = r;
            }

            public int GetValue()
            {
                return 3;
            }
        }

        [Test]
        public void ProxyTest()
        {
            UserData.RegisterProxyType<Proxy, Random>(r => new Proxy(r));

            Script S = new();

            S.Globals["R"] = new Random();
            S.Globals["func"] =
                (Action<Random>)(
                    r =>
                    {
                        Assert.IsNotNull(r);
                        Assert.IsTrue(r is Random);
                    }
                );

            S.DoString(
                @"
				x = R.GetValue();
				func(R);
			"
            );

            Assert.AreEqual(3.0, S.Globals.Get("x").Number);
        }
    }
}
