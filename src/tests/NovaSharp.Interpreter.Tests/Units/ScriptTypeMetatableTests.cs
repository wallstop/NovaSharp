namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptTypeMetatableTests
    {
        [Test]
        public void GetTypeMetatableReturnsNullForUnsupportedTypes()
        {
            Script script = new();
            Assert.That(script.GetTypeMetatable((DataType)999), Is.Null);
        }

        [Test]
        public void SetTypeMetatableThrowsForUnsupportedTypes()
        {
            Script script = new();
            Table table = new(script);

            Assert.Throws<ArgumentException>(() => script.SetTypeMetatable((DataType)(-1), table));
        }

        [Test]
        public void WarmUpInitializesParser()
        {
            Assert.DoesNotThrow(() => Script.WarmUp());
        }
    }
}
