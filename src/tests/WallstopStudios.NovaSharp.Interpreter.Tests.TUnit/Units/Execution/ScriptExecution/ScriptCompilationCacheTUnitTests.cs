namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

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
        [AllLuaVersions]
        public async Task LoadFunctionWithCachingDisabledDoesNotCache(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = false,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "function() return 42 end";
            int initialSourceCount = script.SourceCodeCount;

            DynValue result1 = script.LoadFunction(code, funcFriendlyName: "cached_func");
            DynValue result2 = script.LoadFunction(code, funcFriendlyName: "cached_func");

            await Assert.That(script.Call(result1).Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 2)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LoadFunctionWithCachingEnabledCachesFunctions(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "function() return 42 end";
            int initialSourceCount = script.SourceCodeCount;

            DynValue result1 = script.LoadFunction(code, funcFriendlyName: "cached_func");
            DynValue result2 = script.LoadFunction(code, funcFriendlyName: "cached_func");

            await Assert.That(script.Call(result1).Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(result1).IsNotSameReferenceAs(result2).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("libfunc_cached_func")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LoadFunctionWithDifferentFriendlyNamesCreatesDifferentCacheEntries(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "function() return 42 end";
            int initialSourceCount = script.SourceCodeCount;

            DynValue result1 = script.LoadFunction(code, funcFriendlyName: "first");
            DynValue result2 = script.LoadFunction(code, funcFriendlyName: "second");

            await Assert.That(script.Call(result1).Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 2)
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("libfunc_first")
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount + 1).Name)
                .IsEqualTo("libfunc_second")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CachedLoadFunctionWithDifferentGlobalTablesExecutesCorrectly(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "function() return value end";
            Table globals1 = new(script);
            globals1["value"] = DynValue.NewNumber(10);
            Table globals2 = new(script);
            globals2["value"] = DynValue.NewNumber(20);

            DynValue first = script.LoadFunction(code, globals1, "cached_env");
            DynValue second = script.LoadFunction(code, globals2, "cached_env");

            await Assert.That(script.Call(first).Number).IsEqualTo(10d).ConfigureAwait(false);
            await Assert.That(script.Call(second).Number).IsEqualTo(20d).ConfigureAwait(false);
            await Assert.That(first).IsNotSameReferenceAs(second).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CachedLoadFunctionKeepsNestedClosureUpValuesIndependent(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = """
                function()
                    local counter = 0
                    return function()
                        counter = counter + 1
                        return counter
                    end
                end
                """;

            DynValue firstFactory = script.LoadFunction(code, funcFriendlyName: "nested_counter");
            DynValue secondFactory = script.LoadFunction(code, funcFriendlyName: "nested_counter");
            DynValue firstCounter = script.Call(firstFactory);
            DynValue secondCounter = script.Call(secondFactory);

            await Assert
                .That(firstFactory)
                .IsNotSameReferenceAs(secondFactory)
                .ConfigureAwait(false);
            await Assert.That(firstCounter.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);
            await Assert
                .That(secondCounter.Type)
                .IsEqualTo(DataType.Function)
                .ConfigureAwait(false);
            await Assert.That(script.Call(firstCounter).Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(script.Call(firstCounter).Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert
                .That(script.Call(secondCounter).Number)
                .IsEqualTo(1d)
                .ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CachedLoadFunctionSetFenvKeepsIndependentEnvironmentSlots()
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua51,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "function() return value end";

            DynValue first = script.LoadFunction(code, funcFriendlyName: "cached_setfenv");
            DynValue second = script.LoadFunction(code, funcFriendlyName: "cached_setfenv");
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);

            script.Globals["first"] = first;
            script.Globals["second"] = second;

            DynValue result = script.DoString(
                @"
                local firstEnv = { value = 11 }
                local secondEnv = { value = 22 }
                setfenv(first, firstEnv)
                setfenv(second, secondEnv)
                return first(), second(), getfenv(first) ~= getfenv(second)
                "
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(11d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(22d).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LoadFunctionWithZeroMaxEntriesDoesNotCache(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
                ScriptCacheMaxEntries = 0,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "function() return 42 end";
            int initialSourceCount = script.SourceCodeCount;

            DynValue result1 = script.LoadFunction(code, funcFriendlyName: "zero_cache_func");
            DynValue result2 = script.LoadFunction(code, funcFriendlyName: "zero_cache_func");

            await Assert.That(script.Call(result1).Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 2)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(false)]
        [global::TUnit.Core.Arguments(true)]
        public async Task CachedLoadStringAvoidsTemporaryClosureScaffoldingAllocations(
            bool useFriendlyName
        )
        {
            ScriptOptions options = new() { EnableScriptCaching = true };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "return value";
            string codeFriendlyName = useFriendlyName ? "cached_chunk" : null;

            script.LoadString(code, null, codeFriendlyName);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            const int iterations = 1024;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                script.LoadString(code, null, codeFriendlyName);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            long allocatedPerLoad = allocated / iterations;

            await Assert
                .That(allocatedPerLoad)
                .IsLessThan(256)
                .Because(
                    $"cached {(useFriendlyName ? "named" : "unnamed")} LoadString allocated {allocatedPerLoad} bytes/load across {iterations} iterations ({allocated} total bytes)"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CachedLoadStringClosuresKeepIndependentEnvironmentSlots(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            script.Globals["value"] = DynValue.NewNumber(20);

            DynValue first = script.LoadString("return value");
            DynValue second = script.LoadString("return value");
            Table firstEnvironment = new(script);
            firstEnvironment["value"] = DynValue.NewNumber(10);

            await Assert
                .That(first.Function.GetUpValueName(0))
                .IsEqualTo(WellKnownSymbols.ENV)
                .ConfigureAwait(false);
            first.Function.SetUpValue(0, DynValue.NewTable(firstEnvironment));

            await Assert.That(script.Call(first).Number).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(script.Call(second).Number).IsEqualTo(20).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CachedLoadStringSetFenvKeepsIndependentEnvironmentSlots()
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua51,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);

            DynValue first = script.LoadString("return value");
            DynValue second = script.LoadString("return value");

            script.Globals["first"] = first;
            script.Globals["second"] = second;

            DynValue result = script.DoString(
                @"
                local firstEnv = { value = 11 }
                local secondEnv = { value = 22 }
                setfenv(first, firstEnv)
                setfenv(second, secondEnv)
                return first(), second(), getfenv(first) ~= getfenv(second)
                "
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(22).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(2).ConfigureAwait(false);
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
        public async Task LoadStringWithSameFriendlyNameCachesScripts(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);

            string code = "return 42";
            int initialSourceCount = script.SourceCodeCount;

            DynValue result1 = script.LoadString(code, null, "myScript");
            DynValue result2 = script.LoadString(code, null, "myScript");

            await Assert.That(script.Call(result1).Number).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("myScript")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringWithDifferentFriendlyNamesCreatesDifferentCacheEntries(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);

            string code = "return 42";
            int initialSourceCount = script.SourceCodeCount;

            DynValue result1 = script.LoadString(code, null, "myScript1");
            DynValue result2 = script.LoadString(code, null, "myScript2");

            await Assert.That(script.Call(result1).Number).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 2)
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("myScript1")
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount + 1).Name)
                .IsEqualTo("myScript2")
                .ConfigureAwait(false);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringWithZeroMaxEntriesDoesNotCache(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
                ScriptCacheMaxEntries = 0,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "return 42";

            DynValue result1 = script.LoadString(code);
            DynValue result2 = script.LoadString(code);

            await Assert.That(script.Call(result1).Number).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(42).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(script.SourceCodeCount).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheWithZeroMaxEntriesDoesNotStore()
        {
            ScriptCompilationCache cache = new(maxEntries: 0);

            cache.Store(
                "return 1",
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                entryPointAddress: 11,
                sourceId: 101
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(cache.TryGet("return 1", LuaCompatibilityVersion.Lua54, null, out _))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ScriptWithNegativeCacheMaxEntriesThrows()
        {
            ScriptOptions options = new()
            {
                EnableScriptCaching = true,
                ScriptCacheMaxEntries = -1,
            };

            static void CreateScript(ScriptOptions options)
            {
                Script script = new(CoreModulePresets.Complete, options);
                GC.KeepAlive(script);
            }

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateScript(options)
            );

            await Assert.That(exception.ParamName).IsEqualTo("options").ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("ScriptCacheMaxEntries")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ScriptWithNegativeCacheMaxEntriesAndCachingDisabledDoesNotThrow()
        {
            ScriptOptions options = new()
            {
                EnableScriptCaching = false,
                ScriptCacheMaxEntries = -1,
            };

            Script script = new(CoreModulePresets.Complete, options);

            script.LoadString("return 42");

            await Assert.That(script.CompilationCacheCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheWithNegativeMaxEntriesThrows()
        {
            static void CreateCache()
            {
                ScriptCompilationCache cache = new(maxEntries: -1);
                GC.KeepAlive(cache);
            }

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
                CreateCache
            );

            await Assert.That(exception.ParamName).IsEqualTo("maxEntries").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CachePromotesRecentlyUsedEntryBeforeEviction()
        {
            ScriptCompilationCache cache = new(maxEntries: 2);

            cache.Store(
                "return 1",
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                entryPointAddress: 11,
                sourceId: 101
            );
            cache.Store(
                "return 2",
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                entryPointAddress: 22,
                sourceId: 202
            );

            await Assert
                .That(
                    cache.TryGet(
                        "return 1",
                        LuaCompatibilityVersion.Lua54,
                        null,
                        out CachedChunk promoted
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(promoted._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);

            cache.Store(
                "return 3",
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                entryPointAddress: 33,
                sourceId: 303
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(cache.TryGet("return 2", LuaCompatibilityVersion.Lua54, null, out _))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(cache.TryGet("return 1", LuaCompatibilityVersion.Lua54, null, out _))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(cache.TryGet("return 3", LuaCompatibilityVersion.Lua54, null, out _))
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
        public async Task LoadFunctionAfterCompatibilityVersionChangeCreatesSeparateCacheEntry()
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua51,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "function() return 42 end";

            script.LoadFunction(code, funcFriendlyName: "versioned_func");
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua54;
            script.LoadFunction(code, funcFriendlyName: "versioned_func");

            await Assert.That(script.CompilationCacheCount).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SourceCacheKeyDistinguishesSourceTextsWhenHashesCollide()
        {
            const string firstCode = "return 1";
            const string secondCode = "return 2";
            const int forcedHashCode = 42;

            SourceCacheKey firstKey = new(
                firstCode,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                hashCode: forcedHashCode
            );
            SourceCacheKey secondKey = new(
                secondCode,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                hashCode: forcedHashCode
            );

            await Assert.That(firstKey.GetHashCode()).IsEqualTo(secondKey.GetHashCode());
            await Assert.That(firstKey.Equals(secondKey)).IsFalse().ConfigureAwait(false);

            Dictionary<SourceCacheKey, int> entries = new(2) { [firstKey] = 11, [secondKey] = 22 };

            await Assert.That(entries.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(entries[firstKey]).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(entries[secondKey]).IsEqualTo(22).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SourceCacheKeyDistinguishesFriendlyNamesWhenHashesCollide()
        {
            const string code = "return 1";
            const int forcedHashCode = 42;

            SourceCacheKey firstKey = new(
                code,
                LuaCompatibilityVersion.Lua54,
                "first.lua",
                forcedHashCode
            );
            SourceCacheKey secondKey = new(
                code,
                LuaCompatibilityVersion.Lua54,
                "second.lua",
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
        public async Task SourceCacheKeyDistinguishesCompilationShapesWhenHashesCollide()
        {
            const string code = "return 1";
            const int forcedHashCode = 42;

            SourceCacheKey chunkKey = new(
                code,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                unitKind: ScriptCompilationUnitKind.Chunk,
                usesGlobalEnvironment: false,
                hashCode: forcedHashCode
            );
            SourceCacheKey functionKey = new(
                code,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                unitKind: ScriptCompilationUnitKind.Function,
                usesGlobalEnvironment: true,
                hashCode: forcedHashCode
            );

            await Assert.That(chunkKey.GetHashCode()).IsEqualTo(functionKey.GetHashCode());
            await Assert.That(chunkKey.Equals(functionKey)).IsFalse().ConfigureAwait(false);

            Dictionary<SourceCacheKey, int> entries = new(2)
            {
                [chunkKey] = 11,
                [functionKey] = 22,
            };

            await Assert.That(entries.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(entries[chunkKey]).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(entries[functionKey]).IsEqualTo(22).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheSeparatesSameSourceAcrossCompatibilityVersions()
        {
            ScriptCompilationCache cache = new(maxEntries: 4);
            const string code = "return 1";

            cache.Store(
                code,
                LuaCompatibilityVersion.Lua51,
                sourceName: null,
                entryPointAddress: 11,
                sourceId: 101
            );
            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                entryPointAddress: 22,
                sourceId: 202
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(2).ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(code, LuaCompatibilityVersion.Lua51, null, out CachedChunk lua51)
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(lua51._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(lua51._sourceId).IsEqualTo(101).ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(code, LuaCompatibilityVersion.Lua54, null, out CachedChunk lua54)
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(lua54._entryPointAddress).IsEqualTo(22).ConfigureAwait(false);
            await Assert.That(lua54._sourceId).IsEqualTo(202).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheSeparatesChunksAndFunctionsWithSameSource()
        {
            ScriptCompilationCache cache = new(maxEntries: 4);
            const string code = "return 1";

            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                entryPointAddress: 11,
                sourceId: 101
            );
            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                unitKind: ScriptCompilationUnitKind.Function,
                usesGlobalEnvironment: true,
                entryPointAddress: 22,
                sourceId: 202
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(2).ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(code, LuaCompatibilityVersion.Lua54, null, out CachedChunk chunk)
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(chunk._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(chunk._sourceId).IsEqualTo(101).ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(
                        code,
                        LuaCompatibilityVersion.Lua54,
                        null,
                        ScriptCompilationUnitKind.Function,
                        usesGlobalEnvironment: true,
                        out CachedChunk function
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(function._entryPointAddress).IsEqualTo(22).ConfigureAwait(false);
            await Assert.That(function._sourceId).IsEqualTo(202).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheSeparatesFunctionEnvironmentShapes()
        {
            ScriptCompilationCache cache = new(maxEntries: 4);
            const string code = "function() return value end";

            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                sourceName: "func",
                unitKind: ScriptCompilationUnitKind.Function,
                usesGlobalEnvironment: false,
                entryPointAddress: 11,
                sourceId: 101
            );
            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                sourceName: "func",
                unitKind: ScriptCompilationUnitKind.Function,
                usesGlobalEnvironment: true,
                entryPointAddress: 22,
                sourceId: 202
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(2).ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(
                        code,
                        LuaCompatibilityVersion.Lua54,
                        "func",
                        ScriptCompilationUnitKind.Function,
                        usesGlobalEnvironment: false,
                        out CachedChunk localEnvironment
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(localEnvironment._entryPointAddress)
                .IsEqualTo(11)
                .ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(
                        code,
                        LuaCompatibilityVersion.Lua54,
                        "func",
                        ScriptCompilationUnitKind.Function,
                        usesGlobalEnvironment: true,
                        out CachedChunk globalEnvironment
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(globalEnvironment._entryPointAddress)
                .IsEqualTo(22)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheSeparatesSameSourceAcrossFriendlyNames()
        {
            ScriptCompilationCache cache = new(maxEntries: 4);
            const string code = "return 1";

            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                "first.lua",
                entryPointAddress: 11,
                sourceId: 101
            );
            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                "second.lua",
                entryPointAddress: 22,
                sourceId: 202
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(2).ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(
                        code,
                        LuaCompatibilityVersion.Lua54,
                        "first.lua",
                        out CachedChunk first
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(first._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(first._sourceId).IsEqualTo(101).ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(
                        code,
                        LuaCompatibilityVersion.Lua54,
                        "second.lua",
                        out CachedChunk second
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(second._entryPointAddress).IsEqualTo(22).ConfigureAwait(false);
            await Assert.That(second._sourceId).IsEqualTo(202).ConfigureAwait(false);
            await Assert
                .That(cache.TryGet(code, LuaCompatibilityVersion.Lua54, null, out _))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CachePromotesNamedEntryBeforeEviction()
        {
            ScriptCompilationCache cache = new(maxEntries: 2);
            const string code = "return 1";

            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                "first.lua",
                entryPointAddress: 11,
                sourceId: 101
            );
            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                "second.lua",
                entryPointAddress: 22,
                sourceId: 202
            );

            await Assert
                .That(
                    cache.TryGet(
                        code,
                        LuaCompatibilityVersion.Lua54,
                        "first.lua",
                        out CachedChunk promoted
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(promoted._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);

            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                "third.lua",
                entryPointAddress: 33,
                sourceId: 303
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(cache.TryGet(code, LuaCompatibilityVersion.Lua54, "second.lua", out _))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(cache.TryGet(code, LuaCompatibilityVersion.Lua54, "first.lua", out _))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(cache.TryGet(code, LuaCompatibilityVersion.Lua54, "third.lua", out _))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheClearInvalidatesMostRecentLookup()
        {
            ScriptCompilationCache cache = new(maxEntries: 2);
            const string code = "return 1";

            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                "cached.lua",
                entryPointAddress: 11,
                sourceId: 101
            );

            await Assert
                .That(
                    cache.TryGet(
                        code,
                        LuaCompatibilityVersion.Lua54,
                        "cached.lua",
                        out CachedChunk cached
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(cached._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);

            cache.Clear();

            await Assert.That(cache.ApproximateCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(cache.TryGet(code, LuaCompatibilityVersion.Lua54, "cached.lua", out _))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheEvictionInvalidatesMostRecentLookupWhenEntryIsEvicted()
        {
            ScriptCompilationCache cache = new(maxEntries: 1);
            const string firstCode = "return 1";
            const string secondCode = "return 2";

            cache.Store(
                firstCode,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                entryPointAddress: 11,
                sourceId: 101
            );

            await Assert
                .That(
                    cache.TryGet(
                        firstCode,
                        LuaCompatibilityVersion.Lua54,
                        null,
                        out CachedChunk first
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(first._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);

            cache.Store(
                secondCode,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                entryPointAddress: 22,
                sourceId: 202
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(cache.TryGet(firstCode, LuaCompatibilityVersion.Lua54, null, out _))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(
                    cache.TryGet(
                        secondCode,
                        LuaCompatibilityVersion.Lua54,
                        null,
                        out CachedChunk second
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(second._entryPointAddress).IsEqualTo(22).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheMostRecentLookupDoesNotCrossVersionOrFriendlyName()
        {
            ScriptCompilationCache cache = new(maxEntries: 2);
            const string code = "return 1";
            const string sourceName = "first.lua";

            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                sourceName,
                entryPointAddress: 11,
                sourceId: 101
            );

            await Assert
                .That(
                    cache.TryGet(
                        code,
                        LuaCompatibilityVersion.Lua54,
                        sourceName,
                        out CachedChunk cached
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(cached._sourceId).IsEqualTo(101).ConfigureAwait(false);

            await Assert
                .That(cache.TryGet(code, LuaCompatibilityVersion.Lua53, sourceName, out _))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(cache.TryGet(code, LuaCompatibilityVersion.Lua54, "second.lua", out _))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheFallsBackToValueEqualityForEqualButDistinctStrings()
        {
            ScriptCompilationCache cache = new(maxEntries: 2);
            string storedCode = new("return 1".ToCharArray());
            string lookupCode = new("return 1".ToCharArray());
            string storedSourceName = new("same.lua".ToCharArray());
            string lookupSourceName = new("same.lua".ToCharArray());

            cache.Store(
                storedCode,
                LuaCompatibilityVersion.Lua54,
                storedSourceName,
                entryPointAddress: 11,
                sourceId: 101
            );

            await Assert
                .That(ReferenceEquals(storedCode, lookupCode))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(ReferenceEquals(storedSourceName, lookupSourceName))
                .IsFalse()
                .ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(
                        lookupCode,
                        LuaCompatibilityVersion.Lua54,
                        lookupSourceName,
                        out CachedChunk cached
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(cached._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(cached._sourceId).IsEqualTo(101).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheStoreUpdatesMostRecentLookupForExistingKey()
        {
            ScriptCompilationCache cache = new(maxEntries: 2);
            const string code = "return 1";

            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                entryPointAddress: 11,
                sourceId: 101
            );

            await Assert
                .That(
                    cache.TryGet(code, LuaCompatibilityVersion.Lua54, null, out CachedChunk first)
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(first._entryPointAddress).IsEqualTo(11).ConfigureAwait(false);

            cache.Store(
                code,
                LuaCompatibilityVersion.Lua54,
                sourceName: null,
                entryPointAddress: 22,
                sourceId: 202
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(
                    cache.TryGet(code, LuaCompatibilityVersion.Lua54, null, out CachedChunk updated)
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(updated._entryPointAddress).IsEqualTo(22).ConfigureAwait(false);
            await Assert.That(updated._sourceId).IsEqualTo(202).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CacheStoreUpdatesExistingKeyWithEqualButDistinctStrings()
        {
            ScriptCompilationCache cache = new(maxEntries: 2);
            string firstCode = new("return 1".ToCharArray());
            string secondCode = new("return 1".ToCharArray());
            string firstSourceName = new("same.lua".ToCharArray());
            string secondSourceName = new("same.lua".ToCharArray());

            cache.Store(
                firstCode,
                LuaCompatibilityVersion.Lua54,
                firstSourceName,
                entryPointAddress: 11,
                sourceId: 101
            );
            cache.Store(
                secondCode,
                LuaCompatibilityVersion.Lua54,
                secondSourceName,
                entryPointAddress: 22,
                sourceId: 202
            );

            await Assert.That(cache.ApproximateCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(ReferenceEquals(firstCode, secondCode))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(ReferenceEquals(firstSourceName, secondSourceName))
                .IsFalse()
                .ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(
                        firstCode,
                        LuaCompatibilityVersion.Lua54,
                        firstSourceName,
                        out CachedChunk firstLookup
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(firstLookup._entryPointAddress).IsEqualTo(22).ConfigureAwait(false);
            await Assert.That(firstLookup._sourceId).IsEqualTo(202).ConfigureAwait(false);

            await Assert
                .That(
                    cache.TryGet(
                        secondCode,
                        LuaCompatibilityVersion.Lua54,
                        secondSourceName,
                        out CachedChunk secondLookup
                    )
                )
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(secondLookup._entryPointAddress).IsEqualTo(22).ConfigureAwait(false);
            await Assert.That(secondLookup._sourceId).IsEqualTo(202).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DoStringAfterClearRecompilesInsteadOfUsingMostRecentLookup()
        {
            ScriptOptions options = new() { EnableScriptCaching = true };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = "counter = (counter or 0) + 1; return counter";
            const string sourceName = "clear_public_cached.lua";

            int initialSourceCount = script.SourceCodeCount;
            DynValue first = script.DoString(code, codeFriendlyName: sourceName);

            await Assert.That(first.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);

            script.ClearCompilationCache();

            await Assert.That(script.CompilationCacheCount).IsEqualTo(0).ConfigureAwait(false);

            DynValue second = script.DoString(code, codeFriendlyName: sourceName);

            await Assert.That(second.Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 2)
                .ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }
    }
}
