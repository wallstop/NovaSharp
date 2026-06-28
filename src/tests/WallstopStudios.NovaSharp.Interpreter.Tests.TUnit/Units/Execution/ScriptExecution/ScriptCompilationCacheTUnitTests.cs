namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Tests for the script compilation cache feature.
    /// </summary>
    public sealed class ScriptCompilationCacheTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringWithCachingDisabledDoesNotCache(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = false,
            };
            Script script = new(CoreModulePresets.Complete, options);

            string code = "return 42";

            // Load twice
            DynValue result1 = script.LoadString(code);
            DynValue result2 = script.LoadString(code);

            // Both should work
            await Assert.That(script.Call(result1).Number).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(42).ConfigureAwait(false);

            // Cache should be empty (feature disabled)
            await Assert.That(script.CompilationCacheCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringWithCachingEnabledCachesScripts(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);

            string code = "return 42";

            // Load the same script twice
            DynValue result1 = script.LoadString(code);
            DynValue result2 = script.LoadString(code);

            // Both should work
            await Assert.That(script.Call(result1).Number).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(42).ConfigureAwait(false);

            // Cache should have 1 entry
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringWithDifferentCodeCreatesDifferentCacheEntries(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);

            DynValue result1 = script.LoadString("return 1");
            DynValue result2 = script.LoadString("return 2");
            DynValue result3 = script.LoadString("return 3");

            await Assert.That(script.Call(result1).Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(script.Call(result3).Number).IsEqualTo(3).ConfigureAwait(false);

            // Should have 3 cache entries
            await Assert.That(script.CompilationCacheCount).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringWithFriendlyNameBypassesCache(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);

            string code = "return 42";

            // Load with a friendly name - should bypass cache
            DynValue result1 = script.LoadString(code, null, "myScript1");
            DynValue result2 = script.LoadString(code, null, "myScript2");

            // Both should work
            await Assert.That(script.Call(result1).Number).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(42).ConfigureAwait(false);

            // Cache should be empty because friendly names bypass caching
            await Assert.That(script.CompilationCacheCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ClearCompilationCacheRemovesCachedEntries(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);

            // Load some scripts
            script.LoadString("return 1");
            script.LoadString("return 2");
            script.LoadString("return 3");

            await Assert.That(script.CompilationCacheCount).IsEqualTo(3).ConfigureAwait(false);

            // Clear the cache
            script.ClearCompilationCache();

            await Assert.That(script.CompilationCacheCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CachedScriptProducesSameResultAsUncached(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);

            string code =
                @"
                local sum = 0
                for i = 1, 10 do
                    sum = sum + i
                end
                return sum
            ";

            // First load - not cached
            DynValue result1 = script.LoadString(code);
            double value1 = script.Call(result1).Number;

            // Second load - cached
            DynValue result2 = script.LoadString(code);
            double value2 = script.Call(result2).Number;

            await Assert.That(value1).IsEqualTo(55).ConfigureAwait(false);
            await Assert.That(value2).IsEqualTo(55).ConfigureAwait(false);
            await Assert.That(value1).IsEqualTo(value2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CachedScriptWithDifferentGlobalTableExecutesCorrectly(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);

            string code = "return x + y";

            // Create two different global tables
            Table globals1 = new(script);
            globals1["x"] = DynValue.NewNumber(10);
            globals1["y"] = DynValue.NewNumber(20);

            Table globals2 = new(script);
            globals2["x"] = DynValue.NewNumber(100);
            globals2["y"] = DynValue.NewNumber(200);

            // Load with different global tables
            DynValue result1 = script.LoadString(code, globals1);
            DynValue result2 = script.LoadString(code, globals2);

            // Execute with their respective environments
            double value1 = script.Call(result1).Number;
            double value2 = script.Call(result2).Number;

            await Assert.That(value1).IsEqualTo(30).ConfigureAwait(false);
            await Assert.That(value2).IsEqualTo(300).ConfigureAwait(false);

            // Only one cache entry since code is the same
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CacheRespectsMaxEntriesLimit(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
                ScriptCacheMaxEntries = 5, // Small limit for testing
            };
            Script script = new(CoreModulePresets.Complete, options);

            // Load more scripts than the cache limit
            for (int i = 0; i < 20; i++)
            {
                script.LoadString($"return {i}");
            }

            await Assert.That(script.CompilationCacheCount).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CachePromotesRecentlyUsedEntryBeforeEviction()
        {
            ScriptCompilationCache cache = new(maxEntries: 2);

            cache.Store(
                "return 1",
                LuaCompatibilityVersion.Lua54,
                entryPointAddress: 11,
                sourceId: 101
            );
            cache.Store(
                "return 2",
                LuaCompatibilityVersion.Lua54,
                entryPointAddress: 22,
                sourceId: 202
            );

            await Assert
                .That(
                    cache.TryGet(
                        "return 1",
                        LuaCompatibilityVersion.Lua54,
                        out CachedChunk promoted
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(promoted._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);

            cache.Store(
                "return 3",
                LuaCompatibilityVersion.Lua54,
                entryPointAddress: 33,
                sourceId: 303
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(cache.TryGet("return 2", LuaCompatibilityVersion.Lua54, out _))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(cache.TryGet("return 1", LuaCompatibilityVersion.Lua54, out _))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(cache.TryGet("return 3", LuaCompatibilityVersion.Lua54, out _))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DefaultScriptHasCachingEnabled(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            // Load the same script twice
            string code = "return 123";
            script.LoadString(code);
            script.LoadString(code);

            // Should have caching enabled by default
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadStringAfterCompatibilityVersionChangeCreatesSeparateCacheEntry()
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua51,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "return 42";

            script.LoadString(code);
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua54;
            script.LoadString(code);

            await Assert.That(script.CompilationCacheCount).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SourceCacheKeyDistinguishesSourceTextsWhenHashesCollide()
        {
            const string firstCode = "return 1";
            const string secondCode = "return 2";
            const int forcedHashCode = 42;

            SourceCacheKey firstKey = new(firstCode, LuaCompatibilityVersion.Lua54, forcedHashCode);
            SourceCacheKey secondKey = new(
                secondCode,
                LuaCompatibilityVersion.Lua54,
                forcedHashCode
            );

            await Assert.That(firstKey.GetHashCode()).IsEqualTo(secondKey.GetHashCode());
            await Assert.That(firstKey.Equals(secondKey)).IsFalse().ConfigureAwait(false);

            Dictionary<SourceCacheKey, int> entries = new(2) { [firstKey] = 11, [secondKey] = 22 };

            await Assert.That(entries.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(entries[firstKey]).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(entries[secondKey]).IsEqualTo(22).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheSeparatesSameSourceAcrossCompatibilityVersions()
        {
            ScriptCompilationCache cache = new(maxEntries: 4);
            const string code = "return 1";

            cache.Store(code, LuaCompatibilityVersion.Lua51, entryPointAddress: 11, sourceId: 101);
            cache.Store(code, LuaCompatibilityVersion.Lua54, entryPointAddress: 22, sourceId: 202);

            await Assert.That(cache.ApproximateCount).IsEqualTo(2).ConfigureAwait(false);

            await Assert
                .That(cache.TryGet(code, LuaCompatibilityVersion.Lua51, out CachedChunk lua51))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(lua51._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(lua51._sourceId).IsEqualTo(101).ConfigureAwait(false);

            await Assert
                .That(cache.TryGet(code, LuaCompatibilityVersion.Lua54, out CachedChunk lua54))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(lua54._entryPointAddress).IsEqualTo(22).ConfigureAwait(false);
            await Assert.That(lua54._sourceId).IsEqualTo(202).ConfigureAwait(false);
        }
    }
}
