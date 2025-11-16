namespace NovaSharp.Interpreter.Tests.Units.Debugging
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NUnit.Framework;

    [TestFixture]
    public sealed class WatchItemTests
    {
        [Test]
        public void ToStringIncludesAddressNameValueAndSymbol()
        {
            WatchItem item = new()
            {
                Address = 1,
                BasePtr = 2,
                RetAddress = 3,
                Name = "counter",
                Value = DynValue.NewNumber(42),
                LValue = SymbolRef.Global("counter", SymbolRef.DefaultEnv),
            };

            string formatted = item.ToString();

            Assert.That(
                formatted,
                Is.EqualTo("1:2:3:counter:42:counter : Global / (default _ENV)")
            );
        }

        [Test]
        public void ToStringHandlesNullMembers()
        {
            WatchItem item = new()
            {
                Address = 0,
                BasePtr = 0,
                RetAddress = 0,
                Name = null,
                Value = null,
                LValue = null,
            };

            Assert.That(item.ToString(), Is.EqualTo("0:0:0:(null):(null):(null)"));
        }
    }
}
