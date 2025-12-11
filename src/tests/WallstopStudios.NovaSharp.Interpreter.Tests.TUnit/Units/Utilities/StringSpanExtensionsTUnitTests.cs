namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Utilities
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;

    public sealed class StringSpanExtensionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TrimWhitespaceReturnsOriginalSpanWhenNoWhitespace()
        {
            bool equal = TrimWhitespaceEqualsOriginal("abc");

            await Assert.That(equal).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TrimWhitespaceStripsLeadingAndTrailingWhitespace()
        {
            string trimmed = TrimWhitespaceToString("   hello world\t");

            await Assert.That(trimmed).IsEqualTo("hello world").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TrimWhitespaceReturnsEmptySpanWhenAllWhitespace()
        {
            int length = TrimWhitespaceLength(" \t \r\n ");

            await Assert.That(length).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HasContentReturnsFalseForWhitespaceOnlyInput()
        {
            bool hasContent = SpanHasContent("   \t ");

            await Assert.That(hasContent).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HasContentReturnsTrueWhenAnyNonWhitespaceCharacterExists()
        {
            bool hasContent = SpanHasContent("  a ");

            await Assert.That(hasContent).IsTrue().ConfigureAwait(false);
        }

        private static bool TrimWhitespaceEqualsOriginal(string text)
        {
            ReadOnlySpan<char> span = text.AsSpan();
            return span.TrimWhitespace().SequenceEqual(span);
        }

        private static string TrimWhitespaceToString(string text)
        {
            return text.AsSpan().TrimWhitespace().ToString();
        }

        private static int TrimWhitespaceLength(string text)
        {
            return text.AsSpan().TrimWhitespace().Length;
        }

        private static bool SpanHasContent(string text)
        {
            return text.AsSpan().HasContent();
        }
    }
}
