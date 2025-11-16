namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.CoreLib.StringLib;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StringRangeTests
    {
        [Test]
        public void FromLuaRangeDefaultsStartAndEndWhenNil()
        {
            StringRange range = StringRange.FromLuaRange(DynValue.Nil, DynValue.Nil, 5);

            Assert.Multiple(() =>
            {
                Assert.That(range.Start, Is.EqualTo(1));
                Assert.That(range.End, Is.EqualTo(5));
            });
        }

        [Test]
        public void FromLuaRangeUsesStartWhenEndMissing()
        {
            StringRange range = StringRange.FromLuaRange(DynValue.NewNumber(3), DynValue.Nil);

            Assert.That(range.Start, Is.EqualTo(3));
            Assert.That(range.End, Is.EqualTo(3));
        }

        [Test]
        public void ApplyToStringSupportsNegativeIndices()
        {
            StringRange range = StringRange.FromLuaRange(
                DynValue.NewNumber(-5),
                DynValue.NewNumber(-2)
            );

            string result = range.ApplyToString("NovaSharp");

            Assert.That(result, Is.EqualTo("Shar"));
        }

        [Test]
        public void ApplyToStringClampsIndicesToBounds()
        {
            StringRange range = new(0, 50);

            string result = range.ApplyToString("Lua");

            Assert.That(result, Is.EqualTo("Lua"));
        }

        [Test]
        public void ApplyToStringReturnsEmptyWhenStartExceedsEnd()
        {
            StringRange range = new(5, 2);

            string result = range.ApplyToString("Nova");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void LengthReturnsInclusiveRangeWidth()
        {
            StringRange range = new(2, 6);

            Assert.That(range.Length(), Is.EqualTo(5));
        }

        [Test]
        public void DefaultConstructorInitializesBounds()
        {
            StringRange range = new();

            Assert.Multiple(() =>
            {
                Assert.That(range.Start, Is.EqualTo(0));
                Assert.That(range.End, Is.EqualTo(0));
            });
        }
    }
}
