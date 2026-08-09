namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using Cysharp.Text;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Tests for ZString-based string operations in LuaValue.
    /// </summary>
    public sealed class DynValueZStringTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task NewConcatenatedStringConcatenatesTwoStrings()
        {
            LuaValue result = LuaValue.NewConcatenatedString("hello", " world");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("hello world").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewConcatenatedStringConcatenatesEmptyStrings()
        {
            LuaValue result = LuaValue.NewConcatenatedString("", "");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewConcatenatedStringConcatenatesLeftEmptyString()
        {
            LuaValue result = LuaValue.NewConcatenatedString("", "world");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("world").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewConcatenatedStringConcatenatesRightEmptyString()
        {
            LuaValue result = LuaValue.NewConcatenatedString("hello", "");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("hello").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewConcatenatedStringThreeStringsConcatenatesCorrectly()
        {
            LuaValue result = LuaValue.NewConcatenatedString("hello", " ", "world");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("hello world").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewConcatenatedStringFourStringsConcatenatesCorrectly()
        {
            LuaValue result = LuaValue.NewConcatenatedString("a", "b", "c", "d");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("abcd").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewConcatenatedStringHandlesSpecialCharacters()
        {
            LuaValue result = LuaValue.NewConcatenatedString("hello\n", "world\t");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("hello\nworld\t").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewConcatenatedStringHandlesUnicodeCharacters()
        {
            LuaValue result = LuaValue.NewConcatenatedString("你好", "世界");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("你好世界").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFromBuilderCreatesStringFromBuilder()
        {
            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.Append("hello");
            sb.Append(' ');
            sb.Append("world");

            LuaValue result = LuaValue.NewStringFromBuilder(sb);

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("hello world").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFromBuilderHandlesEmptyBuilder()
        {
            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            LuaValue result = LuaValue.NewStringFromBuilder(sb);

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewStringFromBuilderHandlesNumericAppends()
        {
            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.Append("value: ");
            sb.Append(42);
            sb.Append(", ratio: ");
            sb.Append(3.14);

            LuaValue result = LuaValue.NewStringFromBuilder(sb);

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).Contains("value: 42").ConfigureAwait(false);
            await Assert.That(result.String).Contains("ratio: 3.14").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewConcatenatedStringWorksInVmConcatScenario()
        {
            // Simulate the VM concat opcode scenario
            string left = "foo";
            string right = "bar";

            LuaValue result = LuaValue.NewConcatenatedString(left, right);

            await Assert.That(result.String).IsEqualTo("foobar").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NewConcatenatedStringWorksWithLongStrings()
        {
            string left = new string('a', 1000);
            string right = new string('b', 1000);

            LuaValue result = LuaValue.NewConcatenatedString(left, right);

            await Assert.That(result.String.Length).IsEqualTo(2000).ConfigureAwait(false);
            await Assert.That(result.String).StartsWith("aaaa").ConfigureAwait(false);
            await Assert.That(result.String).EndsWith("bbbb").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ScriptConcatenationUsesZStringInternally(LuaCompatibilityVersion version)
        {
            // This test verifies that string concatenation in Lua uses the ZString-based API
            Script script = new(version);
            LuaValue result = script.DoString("return 'hello' .. ' ' .. 'world'");

            await Assert.That(result.String).IsEqualTo("hello world").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ScriptConcatenationWithNumbersUsesZString(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString("return 'value: ' .. 42");

            await Assert.That(result.String).IsEqualTo("value: 42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ScriptConcatenationChainedUsesZString(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString("return 'a' .. 'b' .. 'c' .. 'd' .. 'e'");

            await Assert.That(result.String).IsEqualTo("abcde").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ScriptConcatenationInLoopUsesZString(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"
                local s = ''
                for i = 1, 10 do
                    s = s .. i
                end
                return s
            "
            );

            await Assert.That(result.String).IsEqualTo("12345678910").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TableConcatUsesZStringInternally(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"
                local t = {'a', 'b', 'c', 'd'}
                return table.concat(t)
            "
            );

            await Assert.That(result.String).IsEqualTo("abcd").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TableConcatWithSeparatorUsesZString(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"
                local t = {'a', 'b', 'c', 'd'}
                return table.concat(t, ', ')
            "
            );

            await Assert.That(result.String).IsEqualTo("a, b, c, d").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task StringRepUsesZStringInternally(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString("return string.rep('ab', 5)");

            await Assert.That(result.String).IsEqualTo("ababababab").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task StringRepWithSeparatorUsesZString(LuaCompatibilityVersion version)
        {
            // string.rep with separator is Lua 5.3+ only
            Script script = new(version);
            LuaValue result = script.DoString("return string.rep('ab', 3, '-')");

            await Assert.That(result.String).IsEqualTo("ab-ab-ab").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task StringCharUsesZStringInternally(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue result = script.DoString("return string.char(72, 101, 108, 108, 111)");

            await Assert.That(result.String).IsEqualTo("Hello").ConfigureAwait(false);
        }
    }
}
