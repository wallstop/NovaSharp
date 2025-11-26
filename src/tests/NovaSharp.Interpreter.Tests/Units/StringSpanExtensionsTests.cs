namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Utilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StringSpanExtensionsTests
    {
        [Test]
        public void TrimWhitespaceReturnsOriginalSpanWhenNoWhitespace()
        {
            ReadOnlySpan<char> span = "abc".AsSpan();

            ReadOnlySpan<char> result = span.TrimWhitespace();

            Assert.That(result.SequenceEqual(span), Is.True);
        }

        [Test]
        public void TrimWhitespaceStripsLeadingAndTrailingWhitespace()
        {
            ReadOnlySpan<char> span = "   hello world\t".AsSpan();

            ReadOnlySpan<char> result = span.TrimWhitespace();

            Assert.That(result.ToString(), Is.EqualTo("hello world"));
        }

        [Test]
        public void TrimWhitespaceReturnsEmptySpanWhenAllWhitespace()
        {
            ReadOnlySpan<char> span = " \t \r\n ".AsSpan();

            ReadOnlySpan<char> result = span.TrimWhitespace();

            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void HasContentReturnsFalseForWhitespaceOnlyInput()
        {
            Assert.That("   \t ".AsSpan().HasContent(), Is.False);
        }

        [Test]
        public void HasContentReturnsTrueWhenAnyNonWhitespaceCharacterExists()
        {
            Assert.That("  a ".AsSpan().HasContent(), Is.True);
        }
    }
}
