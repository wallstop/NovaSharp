namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Spec-oriented acceptance tests for Lua 5.5-specific features.
    /// Based on the Lua 5.5 reference manual (work-in-progress version as of December 2025).
    /// </summary>
    /// <remarks>
    /// Lua 5.5 introduces several new features including:
    /// - Read-only for-loop variables (const by default)
    /// - Enhanced utf8.offset returning both start and end positions
    /// - Improved float printing precision
    /// - table.create for pre-allocation
    ///
    /// Note: Some features like the 'global' keyword and table.create may not yet be implemented
    /// in NovaSharp. These tests document the expected behavior for future implementation.
    ///
    /// NovaSharp uses its own version string for _VERSION ("NovaSharp {version}") rather than
    /// "Lua 5.x", so version detection tests verify compatibility mode rather than version string.
    /// </remarks>
    public sealed class Lua55SpecTUnitTests : LuaSpecTestBase
    {
        // ========================================
        // Version Detection Tests
        // ========================================

        [Test]
        public async Task ScriptReportsLua55CompatibilityVersion()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);

            await Assert
                .That(script.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua55)
                .ConfigureAwait(false);
        }

        /// <remarks>
        /// NovaSharp reports its own version via _VERSION ("NovaSharp {version}") rather than
        /// the standard Lua version string. This is consistent across all compatibility modes.
        /// </remarks>
        [Test]
        public async Task VersionGlobalReportsNovaSharpVersion()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return _VERSION");

            await Assert.That(result.String).Contains("NovaSharp").ConfigureAwait(false);
        }

        // ========================================
        // String Library Tests (Lua 5.5 §6.4)
        // ========================================

        [Test]
        public async Task StringByteDefaultsToFirstCharacter()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return string.byte('Lua')");

            await Assert.That(result.Number).IsEqualTo(76).ConfigureAwait(false);
        }

        [Test]
        public async Task StringCharProducesCorrectOutput()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return string.char(97, 98, 99)");

            await Assert.That(result.String).IsEqualTo("abc").ConfigureAwait(false);
        }

        [Test]
        public async Task StringCharErrorsOnNonIntegerFloat()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString(
                "local ok, err = pcall(string.char, 65.5) return ok, err"
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task StringSubExtractsInclusiveRange()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return string.sub('abcdefg', 2, 4)");

            await Assert.That(result.String).IsEqualTo("bcd").ConfigureAwait(false);
        }

        [Test]
        public async Task StringRepSupportsOptionalSeparator()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return string.rep('ab', 3, '-')");

            await Assert.That(result.String).IsEqualTo("ab-ab-ab").ConfigureAwait(false);
        }

        // ========================================
        // Math Library Tests (Lua 5.5 §6.7)
        // ========================================

        [Test]
        public async Task MathRandomReturnsValueInRange()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            script.DoString("math.randomseed(12345)");
            DynValue result = script.DoString("return math.random()");

            await Assert.That(result.Number).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThan(1).ConfigureAwait(false);
        }

        [Test]
        public async Task MathRandomseedReturnsSeedTuple()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return math.randomseed(12345)");

            // Lua 5.4+ math.randomseed returns the seed tuple
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task MathTointegerConvertsToInteger()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return math.tointeger(42.0)");

            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        public async Task MathTointegerReturnsNilForNonIntegralFloat()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return math.tointeger(42.5)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task MathTypeReturnsIntegerForIntegers()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return math.type(42)");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        [Test]
        public async Task MathTypeReturnsFloatForFloats()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return math.type(42.0)");

            await Assert.That(result.String).IsEqualTo("float").ConfigureAwait(false);
        }

        // ========================================
        // Table Library Tests (Lua 5.5 §6.6)
        // ========================================

        [Test]
        public async Task TableMoveShiftsElementsCorrectly()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString(
                @"
                local t = {1, 2, 3, 4, 5}
                table.move(t, 2, 4, 1)
                return t[1], t[2], t[3]
                "
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(4).ConfigureAwait(false);
        }

        [Test]
        public async Task TablePackCreatesTableWithNField()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString(
                @"
                local t = table.pack(10, 20, 30)
                return t.n, t[1], t[2], t[3]
                "
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(20).ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(30).ConfigureAwait(false);
        }

        [Test]
        public async Task TableUnpackExpandsTable()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString(
                @"
                local function sum3(a, b, c) return a + b + c end
                return sum3(table.unpack({10, 20, 30}))
                "
            );

            await Assert.That(result.Number).IsEqualTo(60).ConfigureAwait(false);
        }

        // ========================================
        // UTF-8 Library Tests (Lua 5.5 §6.5)
        // ========================================

        [Test]
        public async Task Utf8LenReturnsCharacterCount()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return utf8.len('hello')");

            await Assert.That(result.Number).IsEqualTo(5).ConfigureAwait(false);
        }

        [Test]
        public async Task Utf8CharProducesUtf8String()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return utf8.char(72, 101, 108, 108, 111)");

            await Assert.That(result.String).IsEqualTo("Hello").ConfigureAwait(false);
        }

        [Test]
        public async Task Utf8CodepointReturnsCodePoints()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return utf8.codepoint('ABC', 1, 3)");

            await Assert.That(result.Tuple[0].Number).IsEqualTo(65).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(66).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(67).ConfigureAwait(false);
        }

        // ========================================
        // Coroutine Tests (Lua 5.5 §6.2)
        // ========================================

        [Test]
        public async Task CoroutineCreateAndResume()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString(
                @"
                local co = coroutine.create(function(x) return x * 2 end)
                local ok, val = coroutine.resume(co, 21)
                return ok, val
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        public async Task CoroutineStatusReportsCorrectState()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString(
                @"
                local co = coroutine.create(function() end)
                local before = coroutine.status(co)
                coroutine.resume(co)
                local after = coroutine.status(co)
                return before, after
                "
            );

            await Assert.That(result.Tuple[0].String).IsEqualTo("suspended").ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("dead").ConfigureAwait(false);
        }

        // ========================================
        // Integer Semantics Tests (Lua 5.5)
        // ========================================

        [Test]
        public async Task IntegerDivisionOperatorWorks()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return 7 // 3");

            await Assert.That(result.Number).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task BitwiseAndOperatorWorks()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return 0xFF & 0x0F");

            await Assert.That(result.Number).IsEqualTo(15).ConfigureAwait(false);
        }

        [Test]
        public async Task BitwiseOrOperatorWorks()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return 0xF0 | 0x0F");

            await Assert.That(result.Number).IsEqualTo(255).ConfigureAwait(false);
        }

        [Test]
        public async Task BitwiseXorOperatorWorks()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return 0xFF ~ 0x0F");

            await Assert.That(result.Number).IsEqualTo(240).ConfigureAwait(false);
        }

        [Test]
        public async Task BitwiseNotOperatorWorks()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            // Using 32-bit mask for predictable result
            DynValue result = script.DoString("return (~0) & 0xFFFFFFFF");

            await Assert.That(result.Number).IsEqualTo(4294967295).ConfigureAwait(false);
        }

        [Test]
        public async Task LeftShiftOperatorWorks()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return 1 << 4");

            await Assert.That(result.Number).IsEqualTo(16).ConfigureAwait(false);
        }

        [Test]
        public async Task RightShiftOperatorWorks()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return 32 >> 2");

            await Assert.That(result.Number).IsEqualTo(8).ConfigureAwait(false);
        }

        // ========================================
        // Error Handling Tests (Lua 5.5 §6.1)
        // ========================================

        [Test]
        public async Task PcallCatchesErrors()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString(
                @"
                local function bad() error('test error') end
                local ok, msg = pcall(bad)
                return ok, type(msg)
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("string").ConfigureAwait(false);
        }

        [Test]
        public async Task XpcallUsesMessageHandler()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString(
                @"
                local function bad() error('original') end
                local function handler(msg) return 'handled: ' .. tostring(msg) end
                local ok, msg = xpcall(bad, handler)
                return ok, string.find(msg, 'handled:') ~= nil
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
        }

        // ========================================
        // Basic Library Tests (Lua 5.5 §6.1)
        // ========================================

        [Test]
        public async Task ToNumberParsesHexadecimalStringWithoutBase()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            // Per Lua §3.1, tonumber without base parses hex strings with 0x/0X prefix
            DynValue result = script.DoString("return tonumber('0xFF')");

            await Assert.That(result.Number).IsEqualTo(255).ConfigureAwait(false);
        }

        [Test]
        public async Task ToNumberParsesHexadecimalLiteral()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            // Direct hex literals (0xFF) are parsed at lexer level
            DynValue result = script.DoString("return 0xFF");

            await Assert.That(result.Number).IsEqualTo(255).ConfigureAwait(false);
        }

        [Test]
        public async Task ToNumberParsesHexadecimalWithBase()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            // tonumber with explicit base 16 parses hex strings
            DynValue result = script.DoString("return tonumber('FF', 16)");

            await Assert.That(result.Number).IsEqualTo(255).ConfigureAwait(false);
        }

        [Test]
        public async Task ToNumberWithBaseParsesBinary()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return tonumber('1010', 2)");

            await Assert.That(result.Number).IsEqualTo(10).ConfigureAwait(false);
        }

        [Test]
        public async Task TypeFunctionReturnsCorrectTypes()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString(
                @"
                return type(nil), type(true), type(42), type('str'), type({})
                "
            );

            await Assert.That(result.Tuple[0].String).IsEqualTo("nil").ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("boolean").ConfigureAwait(false);
            await Assert.That(result.Tuple[2].String).IsEqualTo("number").ConfigureAwait(false);
            await Assert.That(result.Tuple[3].String).IsEqualTo("string").ConfigureAwait(false);
            await Assert.That(result.Tuple[4].String).IsEqualTo("table").ConfigureAwait(false);
        }

        [Test]
        public async Task SelectReturnsArgumentsFromIndex()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return select(2, 'a', 'b', 'c')");

            await Assert.That(result.Tuple[0].String).IsEqualTo("b").ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("c").ConfigureAwait(false);
        }

        [Test]
        public async Task SelectWithHashReturnsArgumentCount()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua55, CoreModules.PresetComplete);
            DynValue result = script.DoString("return select('#', 'a', 'b', 'c')");

            await Assert.That(result.Number).IsEqualTo(3).ConfigureAwait(false);
        }
    }
}
