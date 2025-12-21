namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Compatibility
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Tests verifying bit32 library availability follows reference Lua semantics:
    /// - Lua 5.1: bit32 did not exist
    /// - Lua 5.2: bit32 is part of the standard library
    /// - Lua 5.3: bit32 was deprecated (not in standard library; native bitwise operators instead)
    /// - Lua 5.4+: bit32 was removed entirely
    ///
    /// NovaSharp correctly provides bit32 only for Lua 5.2 mode and emits a compatibility
    /// warning when scripts attempt to require('bit32') in other modes.
    /// </summary>
    public sealed class Bit32AvailabilityTUnitTests
    {
        private static readonly string FixturesDir = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "LuaFixtures",
            "Bit32AvailabilityTUnitTests"
        );

        /// <summary>
        /// Verifies bit32 global is nil in Lua 5.1 (it didn't exist yet).
        /// </summary>
        [Test]
        public async Task Bit32IsNilInLua51()
        {
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua51 }
            );

            DynValue result = script.DoString("return bit32 == nil");

            await Assert
                .That(result.Boolean)
                .IsTrue()
                .Because("bit32 did not exist in Lua 5.1")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies bit32 global is available as a table in Lua 5.2.
        /// </summary>
        [Test]
        public async Task Bit32IsAvailableInLua52()
        {
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua52 }
            );

            DynValue result = script.DoString("return type(bit32) == 'table'");

            await Assert
                .That(result.Boolean)
                .IsTrue()
                .Because("bit32 is part of the standard library in Lua 5.2")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies bit32 global is nil in Lua 5.3 (deprecated, not in standard library).
        /// </summary>
        [Test]
        public async Task Bit32IsNilInLua53()
        {
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua53 }
            );

            DynValue result = script.DoString("return bit32 == nil");

            await Assert
                .That(result.Boolean)
                .IsTrue()
                .Because("bit32 was deprecated in Lua 5.3 and removed from the standard library")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies bit32 global is nil in Lua 5.4 (removed entirely).
        /// </summary>
        [Test]
        public async Task Bit32IsNilInLua54()
        {
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua54 }
            );

            DynValue result = script.DoString("return bit32 == nil");

            await Assert
                .That(result.Boolean)
                .IsTrue()
                .Because("bit32 was removed in Lua 5.4")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies bit32 global is nil in Lua 5.5 (removed entirely).
        /// </summary>
        [Test]
        public async Task Bit32IsNilInLua55()
        {
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua55 }
            );

            DynValue result = script.DoString("return bit32 == nil");

            await Assert
                .That(result.Boolean)
                .IsTrue()
                .Because("bit32 was removed in Lua 5.4+")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies bit32 functions work correctly when available (Lua 5.2).
        /// </summary>
        [Test]
        public async Task Bit32FunctionsWorkInLua52()
        {
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua52 }
            );

            DynValue result = script.DoString(
                @"
                local band = bit32.band(0xFF, 0x0F)
                local bor = bit32.bor(0xF0, 0x0F)
                local bxor = bit32.bxor(0xFF, 0x0F)
                return band, bor, bxor
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(0x0F).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0xFF).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(0xF0).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies native bitwise operators work in Lua 5.3+ as replacement for bit32.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task NativeBitwiseOperatorsWorkInLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { CompatibilityVersion = version }
            );

            DynValue result = script.DoString(
                @"
                local a = 0xFF
                local b = 0x0F
                local band = a & b
                local bor = a | b
                local bxor = a ~ b
                local bnot = ~0
                local lshift = 1 << 4
                local rshift = 16 >> 2
                return band, bor, bxor, bnot, lshift, rshift
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(6).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo(0x0F)
                .Because("native AND should work")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(0xFF)
                .Because("native OR should work")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].Number)
                .IsEqualTo(0xF0)
                .Because("native XOR should work")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[3].Number)
                .IsEqualTo(-1)
                .Because("native NOT should work")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[4].Number)
                .IsEqualTo(16)
                .Because("native left shift should work")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[5].Number)
                .IsEqualTo(4)
                .Because("native right shift should work")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Comprehensive test: run Lua fixture verifying bit32 availability in 5.2.
        /// </summary>
        [Test]
        public async Task LuaFixtureBit32Available52()
        {
            string fixturePath = Path.Combine(FixturesDir, "bit32_available_52.lua");
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions
                {
                    CompatibilityVersion = LuaCompatibilityVersion.Lua52,
                    ScriptLoader = new FileSystemScriptLoader(),
                }
            );

            DynValue result = script.DoFile(fixturePath);

            await Assert
                .That(result.Boolean)
                .IsTrue()
                .Because("Lua fixture should pass for bit32 availability in Lua 5.2")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Comprehensive test: run Lua fixture verifying bit32 unavailability in 5.1.
        /// </summary>
        [Test]
        public async Task LuaFixtureBit32Unavailable51()
        {
            string fixturePath = Path.Combine(FixturesDir, "bit32_unavailable_51.lua");
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions
                {
                    CompatibilityVersion = LuaCompatibilityVersion.Lua51,
                    ScriptLoader = new FileSystemScriptLoader(),
                }
            );

            DynValue result = script.DoFile(fixturePath);

            await Assert
                .That(result.Boolean)
                .IsTrue()
                .Because("Lua fixture should pass for bit32 unavailability in Lua 5.1")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Comprehensive test: run Lua fixture verifying bit32 unavailability in 5.3+.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task LuaFixtureBit32Unavailable53Plus(LuaCompatibilityVersion version)
        {
            string fixturePath = Path.Combine(FixturesDir, "bit32_unavailable_53plus.lua");
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions
                {
                    CompatibilityVersion = version,
                    ScriptLoader = new FileSystemScriptLoader(),
                }
            );

            DynValue result = script.DoFile(fixturePath);

            await Assert
                .That(result.Boolean)
                .IsTrue()
                .Because($"Lua fixture should pass for bit32 unavailability in {version}")
                .ConfigureAwait(false);
        }
    }
}
