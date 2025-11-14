namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class TableModuleTests
    {
        [Test]
        public void PackPreservesNilAndReportsCount()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local t = table.pack('a', nil, 42)
                return t.n, t[1], t[2], t[3]
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(4));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(3));
                Assert.That(result.Tuple[1].String, Is.EqualTo("a"));
                Assert.That(result.Tuple[2].IsNil(), Is.True);
                Assert.That(result.Tuple[3].Number, Is.EqualTo(42));
            });
        }

        [Test]
        public void UnpackHonorsExplicitBounds()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = { 10, 20, 30, 40 }
                return table.unpack(values, 2, 3)
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(20));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(30));
            });
        }

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }
    }
}
