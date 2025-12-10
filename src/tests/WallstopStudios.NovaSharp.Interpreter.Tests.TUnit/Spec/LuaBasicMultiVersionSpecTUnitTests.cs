namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Spec harness for Lua basic library behaviours (§6.1), currently focused on tonumber base parsing.
    /// </summary>
    public sealed class LuaBasicMultiVersionSpecTUnitTests : LuaSpecTestBase
    {
        [global::TUnit.Core.Test]
        public async Task ToNumberParsesIntegersAcrossSupportedBases()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue tuple = script.DoString(
                @"
                return tonumber('1010', 2),
                       tonumber('-77', 8),
                       tonumber('+1e', 16),
                       tonumber('Z', 36)
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(10);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(-63);
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(30);
            await Assert.That(tuple.Tuple[3].Number).IsEqualTo(35);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberReturnsNilWhenDigitsExceedBase()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue tuple = script.DoString("return tonumber('2', 2), tonumber('g', 16)");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberErrorsWhenBaseIsOutOfRange()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue result = script.DoString(
                "local ok, err = pcall(tonumber, '1', 40) return ok, err"
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("base out of range");
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberErrorsWhenBaseIsFractional()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue result = script.DoString(
                "local ok, err = pcall(tonumber, '10', 2.5) return ok, err"
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("integer");
        }

        // ========================================
        // Hex String Parsing Tests (Lua §3.1 / §6.1)
        // tonumber without base should parse hex strings with 0x/0X prefix
        // This is required by all Lua versions since 5.1
        // ========================================

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexStringWithoutBase()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue result = script.DoString("return tonumber('0xFF')");

            await Assert.That(result.Number).IsEqualTo(255d);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesLowercaseHexStringWithoutBase()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue result = script.DoString("return tonumber('0xff')");

            await Assert.That(result.Number).IsEqualTo(255d);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesUppercaseHexPrefixWithoutBase()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue result = script.DoString("return tonumber('0XFF')");

            await Assert.That(result.Number).IsEqualTo(255d);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesNegativeHexStringWithoutBase()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue result = script.DoString("return tonumber('-0x10')");

            await Assert.That(result.Number).IsEqualTo(-16d);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesPositiveHexStringWithPlusSign()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue result = script.DoString("return tonumber('+0x10')");

            await Assert.That(result.Number).IsEqualTo(16d);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexStringWithWhitespace()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue result = script.DoString("return tonumber('  0xFF  ')");

            await Assert.That(result.Number).IsEqualTo(255d);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberReturnsNilForIncompleteHexString()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            // "0x" without digits should return nil
            DynValue result = script.DoString("return tonumber('0x')");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberReturnsNilForHexStringWithInvalidChars()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            // "0xG" contains invalid hex digit
            DynValue result = script.DoString("return tonumber('0xG')");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesLargeHexStringWithoutBase()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            DynValue result = script.DoString("return tonumber('0xDeAdBeEf')");

            await Assert.That(result.Number).IsEqualTo(3735928559d);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexFloatWithFraction()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            // 0x1.8 = 1 + 8/16 = 1.5, p0 means * 2^0 = 1.5
            DynValue result = script.DoString("return tonumber('0x1.8p0')");

            await Assert.That(result.Number).IsEqualTo(1.5d);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexFloatWithExponent()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            // 0x1p2 = 1 * 2^2 = 4
            DynValue result = script.DoString("return tonumber('0x1p2')");

            await Assert.That(result.Number).IsEqualTo(4d);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexFloatWithNegativeExponent()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            // 0x10p-2 = 16 * 2^(-2) = 16 / 4 = 4
            DynValue result = script.DoString("return tonumber('0x10p-2')");

            await Assert.That(result.Number).IsEqualTo(4d);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberParsesHexFloatWithPositiveExponentSign()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModulePresets.Complete
            );
            // 0x1p+2 = 1 * 2^2 = 4
            DynValue result = script.DoString("return tonumber('0x1p+2')");

            await Assert.That(result.Number).IsEqualTo(4d);
        }
    }
}
