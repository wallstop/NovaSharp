namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Interop.LuaStateInterop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LuaStateInteropToolsTests
    {
        [Test]
        public void IsNumericTypeCoversCommonScalars()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Tools.IsNumericType(123), Is.True);
                Assert.That(Tools.IsNumericType(123.45m), Is.True);
                Assert.That(Tools.IsNumericType("not a number"), Is.False);
            });
        }

        [Test]
        public void IsPositiveHonoursZeroFlagAndCharBranch()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Tools.IsPositive(0, zeroIsPositive: false), Is.False);
                Assert.That(Tools.IsPositive(0, zeroIsPositive: true), Is.True);
                Assert.That(Tools.IsPositive('A', zeroIsPositive: false), Is.True);
            });
        }

        [Test]
        public void UnboxToLongRoundsFloatingPointWhenRequested()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Tools.UnboxToLong(3.6d, round: true), Is.EqualTo(4));
                Assert.That(Tools.UnboxToLong(3.6d, round: false), Is.EqualTo(3));
            });
        }

        [Test]
        public void ReplaceMetaCharsConvertsEscapes()
        {
            string converted = Tools.ReplaceMetaChars("line\\nindent\\tspace\\040!");

            Assert.That(converted, Is.EqualTo("line\nindent\tspace !"));
        }

        [TestCase("%+05d", 42, "+0042")]
        [TestCase("%05d", -7, "-00007")]
        [TestCase("%-6s", "ab", "ab    ")]
        [TestCase("%#08x", 255, "0x0000ff")]
        public void SprintfHandlesFlagsAndAlignment(string format, object value, string expected)
        {
            string formatted = Tools.Sprintf(format, value);
            Assert.That(formatted, Is.EqualTo(expected));
        }

        [Test]
        public void SprintfRespectsPositiveSpaceFlagAndPrecision()
        {
            string formatted = Tools.Sprintf("% .2f", 3.5);
            Assert.That(formatted, Is.EqualTo(" 3.50"));
        }
    }
}
