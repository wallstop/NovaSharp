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
    /// NovaSharp now correctly reports the Lua version via _VERSION (e.g., "Lua 5.5") based on
    /// the active compatibility mode, matching the official Lua interpreter behavior.
    /// </remarks>
    public sealed class Lua55SpecTUnitTests : LuaSpecTestBase
    {
        // ========================================
        // Version Detection Tests
        // ========================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptReportsLua55CompatibilityVersion(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);

            await Assert
                .That(script.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua55)
                .ConfigureAwait(false);
        }

        /// <remarks>
        /// NovaSharp now correctly reports the Lua version via _VERSION (e.g., "Lua 5.5")
        /// based on the active compatibility mode, matching the official Lua interpreter behavior.
        /// </remarks>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task VersionGlobalReportsLuaVersion(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return _VERSION");

            await Assert.That(result.String).IsEqualTo("Lua 5.5").ConfigureAwait(false);
        }

        // ========================================
        // String Library Tests (Lua 5.5 §6.4)
        // ========================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StringByteDefaultsToFirstCharacter(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.byte('Lua')");

            await Assert.That(result.Number).IsEqualTo(76).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StringCharProducesCorrectOutput(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.char(97, 98, 99)");

            await Assert.That(result.String).IsEqualTo("abc").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StringCharErrorsOnNonIntegerFloat(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StringSubExtractsInclusiveRange(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.sub('abcdefg', 2, 4)");

            await Assert.That(result.String).IsEqualTo("bcd").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StringRepSupportsOptionalSeparator(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.rep('ab', 3, '-')");

            await Assert.That(result.String).IsEqualTo("ab-ab-ab").ConfigureAwait(false);
        }

        // ========================================
        // Math Library Tests (Lua 5.5 §6.7)
        // ========================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathRandomReturnsValueInRange(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            script.DoString("math.randomseed(12345)");
            DynValue result = script.DoString("return math.random()");

            await Assert.That(result.Number).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThan(1).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathRandomseedReturnsSeedTuple(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.randomseed(12345)");

            // Lua 5.4+ math.randomseed returns the seed tuple
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathTointegerConvertsToInteger(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(42.0)");

            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathTointegerReturnsNilForNonIntegralFloat(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(42.5)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathTypeReturnsIntegerForIntegers(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(42)");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathTypeReturnsFloatForFloats(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(42.0)");

            await Assert.That(result.String).IsEqualTo("float").ConfigureAwait(false);
        }

        // ========================================
        // Table Library Tests (Lua 5.5 §6.6)
        // ========================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableMoveShiftsElementsCorrectly(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TablePackCreatesTableWithNField(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableUnpackExpandsTable(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task Utf8LenReturnsCharacterCount(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return utf8.len('hello')");

            await Assert.That(result.Number).IsEqualTo(5).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task Utf8CharProducesUtf8String(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return utf8.char(72, 101, 108, 108, 111)");

            await Assert.That(result.String).IsEqualTo("Hello").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task Utf8CodepointReturnsCodePoints(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return utf8.codepoint('ABC', 1, 3)");

            await Assert.That(result.Tuple[0].Number).IsEqualTo(65).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(66).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(67).ConfigureAwait(false);
        }

        // ========================================
        // Coroutine Tests (Lua 5.5 §6.2)
        // ========================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CoroutineCreateAndResume(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CoroutineStatusReportsCorrectState(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IntegerDivisionOperatorWorks(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 7 // 3");

            await Assert.That(result.Number).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task BitwiseAndOperatorWorks(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 0xFF & 0x0F");

            await Assert.That(result.Number).IsEqualTo(15).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task BitwiseOrOperatorWorks(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 0xF0 | 0x0F");

            await Assert.That(result.Number).IsEqualTo(255).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task BitwiseXorOperatorWorks(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 0xFF ~ 0x0F");

            await Assert.That(result.Number).IsEqualTo(240).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task BitwiseNotOperatorWorks(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            // Using 32-bit mask for predictable result
            DynValue result = script.DoString("return (~0) & 0xFFFFFFFF");

            await Assert.That(result.Number).IsEqualTo(4294967295).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LeftShiftOperatorWorks(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 1 << 4");

            await Assert.That(result.Number).IsEqualTo(16).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RightShiftOperatorWorks(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 32 >> 2");

            await Assert.That(result.Number).IsEqualTo(8).ConfigureAwait(false);
        }

        // ========================================
        // Error Handling Tests (Lua 5.5 §6.1)
        // ========================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PcallCatchesErrors(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task XpcallUsesMessageHandler(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexadecimalStringWithoutBase(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            // Per Lua §3.1, tonumber without base parses hex strings with 0x/0X prefix
            DynValue result = script.DoString("return tonumber('0xFF')");

            await Assert.That(result.Number).IsEqualTo(255).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexadecimalLiteral(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            // Direct hex literals (0xFF) are parsed at lexer level
            DynValue result = script.DoString("return 0xFF");

            await Assert.That(result.Number).IsEqualTo(255).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberParsesHexadecimalWithBase(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            // tonumber with explicit base 16 parses hex strings
            DynValue result = script.DoString("return tonumber('FF', 16)");

            await Assert.That(result.Number).IsEqualTo(255).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ToNumberWithBaseParsesBinary(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return tonumber('1010', 2)");

            await Assert.That(result.Number).IsEqualTo(10).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TypeFunctionReturnsCorrectTypes(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelectReturnsArgumentsFromIndex(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return select(2, 'a', 'b', 'c')");

            await Assert.That(result.Tuple[0].String).IsEqualTo("b").ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("c").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelectWithHashReturnsArgumentCount(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return select('#', 'a', 'b', 'c')");

            await Assert.That(result.Number).IsEqualTo(3).ConfigureAwait(false);
        }
    }
}
