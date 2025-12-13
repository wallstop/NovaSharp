namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions.Extensions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Tests for the <c>__lt</c> metamethod fallback behavior when <c>__le</c> is not defined.
    /// <para>
    /// In Lua 5.1 through 5.4, when <c>__le</c> is not defined, Lua falls back to using
    /// <c>__lt</c> with swapped arguments (i.e., <c>a &lt;= b</c> becomes <c>not (b &lt; a)</c>).
    /// </para>
    /// <para>
    /// In Lua 5.5, this fallback was removed (as documented in Lua 5.4 manual §8.1, but
    /// actually removed in 5.5). When <c>__le</c> is not defined, the comparison fails.
    /// </para>
    /// <para>
    /// Reference: Lua 5.4 manual §8.1 states "The use of the <c>__lt</c> metamethod to
    /// emulate <c>__le</c> has been removed." However, testing against actual Lua 5.4.4
    /// shows the fallback still works. Lua 5.5 actually removes it.
    /// </para>
    /// <para>
    /// <c>LuaCompatibilityVersion.Latest</c> follows the current NovaSharp target (Lua 5.4.x),
    /// so the fallback is allowed. This behavior will change when NovaSharp targets Lua 5.5+.
    /// </para>
    /// </summary>
    public sealed class MetamethodLtLeFallbackTUnitTests
    {
        private const string ScriptWithOnlyLtMetamethod =
            @"
            local mt = {
                __lt = function(a, b) return a.value < b.value end
                -- Note: __le is intentionally NOT defined
            }
            local a = setmetatable({value = 1}, mt)
            local b = setmetatable({value = 2}, mt)
            return a <= b  -- This uses __lt fallback in Lua 5.1-5.4
            ";

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        public async Task LtFallbackToLeWorksInLua51Through54(LuaCompatibilityVersion version)
        {
            Script script = new(new ScriptOptions { CompatibilityVersion = version });
            DynValue result = script.DoString(ScriptWithOnlyLtMetamethod);

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task LtFallbackToLeFailsInLua55()
        {
            Script script = new(
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua55 }
            );

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(() =>
                    Task.FromResult(script.DoString(ScriptWithOnlyLtMetamethod))
                )
                .ConfigureAwait(false);

            // The error should indicate a comparison error
            await Assert.That(ex.Message).Contains("compare").ConfigureAwait(false);
        }

        [Test]
        public async Task LtFallbackToLeWorksInLatestMode()
        {
            // Latest mode follows the current NovaSharp target (Lua 5.4.x), which allows the fallback.
            // When NovaSharp targets Lua 5.5+, this test should be updated to expect failure.
            Script script = new(
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Latest }
            );
            DynValue result = script.DoString(ScriptWithOnlyLtMetamethod);

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task LeMetamethodWorksInAllVersions(LuaCompatibilityVersion version)
        {
            // When __le IS defined, it should work in all versions
            string scriptCode =
                @"
                local mt = {
                    __lt = function(a, b) return a.value < b.value end,
                    __le = function(a, b) return a.value <= b.value end
                }
                local a = setmetatable({value = 1}, mt)
                local b = setmetatable({value = 2}, mt)
                return a <= b
                ";

            Script script = new(new ScriptOptions { CompatibilityVersion = version });
            DynValue result = script.DoString(scriptCode);

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task LtMetamethodWorksInAllVersions(LuaCompatibilityVersion version)
        {
            // __lt should work in all versions (it's the < operator, not <=)
            string scriptCode =
                @"
                local mt = {
                    __lt = function(a, b) return a.value < b.value end
                    -- __le intentionally NOT defined
                }
                local a = setmetatable({value = 1}, mt)
                local b = setmetatable({value = 2}, mt)
                return a < b  -- Using < operator, which uses __lt directly
                ";

            Script script = new(new ScriptOptions { CompatibilityVersion = version });
            DynValue result = script.DoString(scriptCode);

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task LtFallbackCorrectlyInvertsResult()
        {
            // When using __lt fallback for <=, the result should be: not (b < a)
            // So if a.value=2 and b.value=1, then a <= b should be false
            // because __lt(b, a) = (1 < 2) = true, so not(true) = false
            string scriptCode =
                @"
                local mt = {
                    __lt = function(a, b) return a.value < b.value end
                    -- __le intentionally NOT defined
                }
                local a = setmetatable({value = 2}, mt)
                local b = setmetatable({value = 1}, mt)
                return a <= b  -- Should be false: not (1 < 2) = not true = false
                ";

            Script script = new(
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua54 }
            );
            DynValue result = script.DoString(scriptCode);

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task LtFallbackHandlesEqualValues()
        {
            // When a.value == b.value, a <= b should be true
            // Using fallback: not (b < a) = not (2 < 2) = not false = true
            string scriptCode =
                @"
                local mt = {
                    __lt = function(a, b) return a.value < b.value end
                    -- __le intentionally NOT defined
                }
                local a = setmetatable({value = 2}, mt)
                local b = setmetatable({value = 2}, mt)
                return a <= b  -- Should be true: not (2 < 2) = not false = true
                ";

            Script script = new(
                new ScriptOptions { CompatibilityVersion = LuaCompatibilityVersion.Lua54 }
            );
            DynValue result = script.DoString(scriptCode);

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }
    }
}
