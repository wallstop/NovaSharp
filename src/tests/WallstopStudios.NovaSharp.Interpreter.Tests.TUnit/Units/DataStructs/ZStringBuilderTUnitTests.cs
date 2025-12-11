namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataStructs
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// Tests for ZStringBuilder utility methods.
    /// </summary>
    public sealed class ZStringBuilderTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConcatTwoStringsReturnsExpected()
        {
            string result = ZStringBuilder.Concat("hello", " world");

            await Assert.That(result).IsEqualTo("hello world").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConcatThreeStringsReturnsExpected()
        {
            string result = ZStringBuilder.Concat("a", "b", "c");

            await Assert.That(result).IsEqualTo("abc").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConcatFourStringsReturnsExpected()
        {
            string result = ZStringBuilder.Concat("a", "b", "c", "d");

            await Assert.That(result).IsEqualTo("abcd").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConcatWithNumbersReturnsExpected()
        {
            string result = ZStringBuilder.Concat("value: ", 42);

            await Assert.That(result).IsEqualTo("value: 42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConcatWithMixedTypesReturnsExpected()
        {
            string result = ZStringBuilder.Concat("value: ", 42, ", flag: ", true);

            await Assert.That(result).IsEqualTo("value: 42, flag: True").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConcatEmptyStringsReturnsEmpty()
        {
            string result = ZStringBuilder.Concat("", "");

            await Assert.That(result).IsEqualTo("").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatOneArgumentReturnsExpected()
        {
            string result = ZStringBuilder.Format("Hello, {0}!", "world");

            await Assert.That(result).IsEqualTo("Hello, world!").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatTwoArgumentsReturnsExpected()
        {
            string result = ZStringBuilder.Format("{0} + {1} = 3", 1, 2);

            await Assert.That(result).IsEqualTo("1 + 2 = 3").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatThreeArgumentsReturnsExpected()
        {
            string result = ZStringBuilder.Format("{0}, {1}, {2}", "a", "b", "c");

            await Assert.That(result).IsEqualTo("a, b, c").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task JoinWithCharSeparatorReturnsExpected()
        {
            string[] values = { "a", "b", "c" };

            string result = ZStringBuilder.Join(':', values);

            await Assert.That(result).IsEqualTo("a:b:c").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task JoinWithStringSeparatorReturnsExpected()
        {
            string[] values = { "a", "b", "c" };

            string result = ZStringBuilder.Join(", ", values);

            await Assert.That(result).IsEqualTo("a, b, c").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task JoinEmptyArrayReturnsEmpty()
        {
            string[] values = System.Array.Empty<string>();

            string result = ZStringBuilder.Join(':', values);

            await Assert.That(result).IsEqualTo("").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task JoinSingleElementReturnsElement()
        {
            string[] values = { "only" };

            string result = ZStringBuilder.Join(':', values);

            await Assert.That(result).IsEqualTo("only").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task JoinWithNumbersReturnsExpected()
        {
            int[] values = { 1, 2, 3, 4, 5 };

            string result = ZStringBuilder.Join('-', values);

            await Assert.That(result).IsEqualTo("1-2-3-4-5").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CreateReturnsDisposableBuilder()
        {
            using Cysharp.Text.Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.Append("test");

            string result = sb.ToString();

            await Assert.That(result).IsEqualTo("test").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CreateNestedReturnsDisposableBuilder()
        {
            using Cysharp.Text.Utf16ValueStringBuilder sb = ZStringBuilder.CreateNested();
            sb.Append("nested");

            string result = sb.ToString();

            await Assert.That(result).IsEqualTo("nested").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CreateNonNestedReturnsDisposableBuilder()
        {
            using Cysharp.Text.Utf16ValueStringBuilder sb = ZStringBuilder.CreateNonNested();
            sb.Append("non-nested");

            string result = sb.ToString();

            await Assert.That(result).IsEqualTo("non-nested").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CreateUtf8ReturnsDisposableBuilder()
        {
            using Cysharp.Text.Utf8ValueStringBuilder sb = ZStringBuilder.CreateUtf8();
            sb.Append("utf8");

            // Get the bytes and convert to string for comparison
            byte[] bytes = sb.AsSpan().ToArray();
            string result = System.Text.Encoding.UTF8.GetString(bytes);

            await Assert.That(result).IsEqualTo("utf8").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CreateUtf8NestedReturnsDisposableBuilder()
        {
            using Cysharp.Text.Utf8ValueStringBuilder sb = ZStringBuilder.CreateUtf8Nested();
            sb.Append("utf8-nested");

            byte[] bytes = sb.AsSpan().ToArray();
            string result = System.Text.Encoding.UTF8.GetString(bytes);

            await Assert.That(result).IsEqualTo("utf8-nested").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NestedBuildersWorkCorrectly()
        {
            // Test that nested builders don't interfere with each other
            string outer;
            using (Cysharp.Text.Utf16ValueStringBuilder sbOuter = ZStringBuilder.CreateNested())
            {
                sbOuter.Append("outer-");

                string inner;
                using (Cysharp.Text.Utf16ValueStringBuilder sbInner = ZStringBuilder.CreateNested())
                {
                    sbInner.Append("inner");
                    inner = sbInner.ToString();
                }

                sbOuter.Append(inner);
                outer = sbOuter.ToString();
            }

            await Assert.That(outer).IsEqualTo("outer-inner").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BuilderAppendsCharsCorrectly()
        {
            using Cysharp.Text.Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.Append('H');
            sb.Append('e');
            sb.Append('l');
            sb.Append('l');
            sb.Append('o');

            string result = sb.ToString();

            await Assert.That(result).IsEqualTo("Hello").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BuilderAppendsRepeatedCharsCorrectly()
        {
            using Cysharp.Text.Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.Append('=', 10);

            string result = sb.ToString();

            await Assert.That(result).IsEqualTo("==========").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BuilderAppendLineWorks()
        {
            using Cysharp.Text.Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.AppendLine("line1");
            sb.AppendLine("line2");

            string result = sb.ToString();

            await Assert.That(result).Contains("line1").ConfigureAwait(false);
            await Assert.That(result).Contains("line2").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BuilderHandlesLargeStrings()
        {
            using Cysharp.Text.Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            for (int i = 0; i < 10000; i++)
            {
                sb.Append("x");
            }

            string result = sb.ToString();

            await Assert.That(result.Length).IsEqualTo(10000).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BuilderHandlesUnicodeCorrectly()
        {
            using Cysharp.Text.Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.Append("你好");
            sb.Append("世界");

            string result = sb.ToString();

            await Assert.That(result).IsEqualTo("你好世界").ConfigureAwait(false);
        }
    }
}
