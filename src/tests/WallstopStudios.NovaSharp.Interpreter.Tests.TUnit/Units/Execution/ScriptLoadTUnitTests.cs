namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Tests for Script loading, stream handling, and file operations.
    /// Targets uncovered branches in Script.cs loading/dump paths.
    /// </summary>
    public sealed class ScriptLoadTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFunctionWithExplicitFriendlyName(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);

            DynValue result = script.LoadFunction(
                "function() return 42 end",
                globalTable: null,
                funcFriendlyName: "custom_func_name"
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);

            DynValue callResult = script.Call(result);
            await Assert.That(callResult.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFunctionWithNullGlobalTableUsesDefaultGlobals(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            DynValue result = script.LoadFunction("function() return 'ok' end", globalTable: null);

            await Assert.That(result.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);

            DynValue callResult = script.Call(result);
            await Assert.That(callResult.String).IsEqualTo("ok").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringWithNullCodeThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.LoadString(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("code").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStreamWithNullStreamThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.LoadStream(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("stream").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileWithNullFilenameThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.LoadFile(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("filename").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileWithByteArrayLoader(LuaCompatibilityVersion version)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes("return 99");
            ByteArrayScriptLoader loader = new(bytes);
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            DynValue result = script.LoadFile("dummy.lua");
            DynValue callResult = script.Call(result);

            await Assert.That(callResult.Number).IsEqualTo(99d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileWithStreamLoader(LuaCompatibilityVersion version)
        {
            string code = "return 123";
            StreamScriptLoader loader = new(code);
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            DynValue result = script.LoadFile("dummy.lua");
            DynValue callResult = script.Call(result);

            await Assert.That(callResult.Number).IsEqualTo(123d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileWithSameFilenameUsesNamedCompilationCache(
            LuaCompatibilityVersion version
        )
        {
            CountingStringScriptLoader loader = new("return 124");
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
                ScriptLoader = loader,
            };
            Script script = new(CoreModulePresets.Complete, options);
            int initialSourceCount = script.SourceCodeCount;

            DynValue result1 = script.LoadFile("cached.lua");
            DynValue result2 = script.LoadFile("cached.lua");

            await Assert.That(script.Call(result1).Number).IsEqualTo(124d).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(124d).ConfigureAwait(false);
            await Assert.That(loader.LoadCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("cached.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileWithDifferentFilenamesKeepsSeparateNamedCacheEntries(
            LuaCompatibilityVersion version
        )
        {
            CountingStringScriptLoader loader = new("return 125");
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
                ScriptLoader = loader,
            };
            Script script = new(CoreModulePresets.Complete, options);
            int initialSourceCount = script.SourceCodeCount;

            DynValue result1 = script.LoadFile("first.lua");
            DynValue result2 = script.LoadFile("second.lua");

            await Assert.That(script.Call(result1).Number).IsEqualTo(125d).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(125d).ConfigureAwait(false);
            await Assert.That(loader.LoadCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 2)
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("first.lua")
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount + 1).Name)
                .IsEqualTo("second.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStreamWithSameFriendlyNameUsesNamedCompilationCache(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            int initialSourceCount = script.SourceCodeCount;
            byte[] code = System.Text.Encoding.UTF8.GetBytes("return 126");

            using MemoryStream stream1 = new(code);
            using MemoryStream stream2 = new(code);
            DynValue result1 = script.LoadStream(stream1, codeFriendlyName: "stream.lua");
            DynValue result2 = script.LoadStream(stream2, codeFriendlyName: "stream.lua");

            await Assert.That(script.Call(result1).Number).IsEqualTo(126d).ConfigureAwait(false);
            await Assert.That(script.Call(result2).Number).IsEqualTo(126d).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("stream.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DoStringWithSameSourceUsesCompilationCache(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            int initialSourceCount = script.SourceCodeCount;

            DynValue result1 = script.DoString("return 128", codeFriendlyName: "cached_do.lua");
            DynValue result2 = script.DoString("return 128", codeFriendlyName: "cached_do.lua");

            await Assert.That(result1.Number).IsEqualTo(128d).ConfigureAwait(false);
            await Assert.That(result2.Number).IsEqualTo(128d).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("cached_do.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DoStringCacheHitUsesProvidedGlobalTable(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            Table firstGlobals = new(script);
            Table secondGlobals = new(script);
            firstGlobals.Set("marker", DynValue.FromNumber(41));
            secondGlobals.Set("marker", DynValue.FromNumber(42));

            DynValue result1 = script.DoString(
                "return marker",
                firstGlobals,
                codeFriendlyName: "globals.lua"
            );
            DynValue result2 = script.DoString(
                "return marker",
                secondGlobals,
                codeFriendlyName: "globals.lua"
            );

            await Assert.That(result1.Number).IsEqualTo(41d).ConfigureAwait(false);
            await Assert.That(result2.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DoFileWithSameFilenameUsesCompilationCache(
            LuaCompatibilityVersion version
        )
        {
            CountingStringScriptLoader loader = new("counter = (counter or 0) + 1; return counter");
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
                ScriptLoader = loader,
            };
            Script script = new(CoreModulePresets.Complete, options);
            int initialSourceCount = script.SourceCodeCount;

            DynValue result1 = script.DoFile("cached_do_file.lua");
            DynValue result2 = script.DoFile("cached_do_file.lua");

            await Assert.That(result1.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result2.Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(loader.LoadCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("cached_do_file.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DoFileCacheHitUsesProvidedGlobalTable(LuaCompatibilityVersion version)
        {
            CountingStringScriptLoader loader = new("return marker");
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
                ScriptLoader = loader,
            };
            Script script = new(CoreModulePresets.Complete, options);
            Table firstGlobals = new(script);
            Table secondGlobals = new(script);
            firstGlobals.Set("marker", DynValue.FromNumber(41));
            secondGlobals.Set("marker", DynValue.FromNumber(42));

            DynValue result1 = script.DoFile("globals_file.lua", firstGlobals);
            DynValue result2 = script.DoFile("globals_file.lua", secondGlobals);

            await Assert.That(result1.Number).IsEqualTo(41d).ConfigureAwait(false);
            await Assert.That(result2.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(loader.LoadCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DoFileCachedRuntimeErrorUsesFriendlyName(LuaCompatibilityVersion version)
        {
            CountingStringScriptLoader loader = new("local missing = nil; return missing()");
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
                ScriptLoader = loader,
            };
            Script script = new(CoreModulePresets.Complete, options);
            int initialSourceCount = script.SourceCodeCount;

            _ = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoFile("physical_name.lua", codeFriendlyName: "friendly_do_file.lua")
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoFile("physical_name.lua", codeFriendlyName: "friendly_do_file.lua")
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("friendly_do_file.lua")
                .ConfigureAwait(false);
            await Assert.That(loader.LoadCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileStringExecutesRepeatedlyWithoutGrowingSources(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            int initialSourceCount = script.SourceCodeCount;

            CompiledScript compiled = script.CompileString(
                "counter = (counter or 0) + 1; return counter",
                codeFriendlyName: "compiled_counter.lua"
            );

            DynValue first = compiled.Execute();
            DynValue second = compiled.Execute();

            await Assert.That(compiled.IsValid).IsTrue().ConfigureAwait(false);
            await Assert.That(compiled.Script).IsSameReferenceAs(script).ConfigureAwait(false);
            await Assert
                .That(compiled.Function.Type)
                .IsEqualTo(DataType.Function)
                .ConfigureAwait(false);
            await Assert.That(first.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileStringExecutePreservesDebugFrameFunctionIdentity(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileString(
                """
                local info = debug.getinfo(1, "fS")
                local funcInfo = debug.getinfo(info.func, "S")
                local identity = info.func == expected and "same" or "different"
                return identity .. ":" .. type(info.func) .. ":" .. info.what .. ":" .. funcInfo.short_src
                """,
                codeFriendlyName: "compiled_debug.lua"
            );
            script.Globals.Set("expected", compiled.Function);

            DynValue result = compiled.Execute();

            await Assert
                .That(result.String)
                .IsEqualTo("same:function:Lua:compiled_debug.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua51)]
        public async Task CompileStringExecuteSupportsLua51SetfenvFrame(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.Globals.Set("marker", DynValue.FromNumber(5));
            CompiledScript compiled = script.CompileString(
                """
                local before = getfenv(1).marker
                setfenv(1, { marker = 99, getfenv = getfenv, setfenv = setfenv })
                return before * 100 + getfenv(1).marker
                """,
                codeFriendlyName: "compiled_setfenv.lua"
            );

            DynValue result = compiled.Execute();

            await Assert.That(result.Number).IsEqualTo(599d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileStreamExecutesRepeatedlyWithoutGrowingSources(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            int initialSourceCount = script.SourceCodeCount;
            byte[] code = System.Text.Encoding.UTF8.GetBytes(
                "counter = (counter or 0) + 1; return counter"
            );
            using MemoryStream stream = new(code);

            CompiledScript compiled = script.CompileStream(
                stream,
                codeFriendlyName: "compiled_stream.lua"
            );

            DynValue first = compiled.Execute();
            DynValue second = compiled.Execute();

            await Assert.That(first.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("compiled_stream.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFileExecutesRepeatedlyWithoutReloadingFile(
            LuaCompatibilityVersion version
        )
        {
            CountingStringScriptLoader loader = new("counter = (counter or 0) + 1; return counter");
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
                ScriptLoader = loader,
            };
            Script script = new(CoreModulePresets.Complete, options);
            int initialSourceCount = script.SourceCodeCount;

            CompiledScript compiled = script.CompileFile("compiled_file.lua");

            DynValue first = compiled.Execute();
            DynValue second = compiled.Execute();

            await Assert.That(first.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(loader.LoadCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 1)
                .ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(script.GetSourceCode(initialSourceCount).Name)
                .IsEqualTo("compiled_file.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFileUsesProvidedGlobalContext(LuaCompatibilityVersion version)
        {
            CountingStringScriptLoader loader = new("return marker");
            ScriptOptions options = new() { CompatibilityVersion = version, ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);
            Table globals = new(script);
            globals.Set("marker", DynValue.FromNumber(73));

            CompiledScript compiled = script.CompileFile("compiled_globals.lua", globals);
            DynValue result = compiled.Execute();

            await Assert.That(result.Number).IsEqualTo(73d).ConfigureAwait(false);
            await Assert.That(loader.LoadCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFileRuntimeErrorUsesFriendlyName(LuaCompatibilityVersion version)
        {
            CountingStringScriptLoader loader = new("local missing = nil; return missing()");
            ScriptOptions options = new() { CompatibilityVersion = version, ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            CompiledScript compiled = script.CompileFile(
                "physical_name.lua",
                friendlyFilename: "friendly_name.lua"
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                compiled.Execute()
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("friendly_name.lua")
                .ConfigureAwait(false);
            await Assert.That(loader.LoadCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileStreamDoesNotDisposeCallerOwnedStream(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            byte[] code = System.Text.Encoding.UTF8.GetBytes("return 91");
            using TrackingMemoryStream stream = new(code);

            CompiledScript compiled = script.CompileStream(
                stream,
                codeFriendlyName: "caller_stream.lua"
            );
            DynValue result = compiled.Execute();

            await Assert.That(result.Number).IsEqualTo(91d).ConfigureAwait(false);
            await Assert.That(stream.IsDisposed).IsFalse().ConfigureAwait(false);
            await Assert.That(stream.CanRead).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileStreamExecutesAfterCallerDisposesSourceStream(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            byte[] code = System.Text.Encoding.UTF8.GetBytes("return 93");
            CompiledScript compiled;
            TrackingMemoryStream stream;

            using (stream = new TrackingMemoryStream(code))
            {
                compiled = script.CompileStream(
                    stream,
                    codeFriendlyName: "disposed_caller_stream.lua"
                );
            }

            DynValue result = compiled.Execute();

            await Assert.That(stream.IsDisposed).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(93d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFileDisposesLoaderReturnedStream(LuaCompatibilityVersion version)
        {
            TrackingStreamScriptLoader loader = new("return 92");
            ScriptOptions options = new() { CompatibilityVersion = version, ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            CompiledScript compiled = script.CompileFile("stream_loader.lua");
            DynValue result = compiled.Execute();

            await Assert.That(result.Number).IsEqualTo(92d).ConfigureAwait(false);
            await Assert.That(loader.LoadCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(loader.LastStream.IsDisposed).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionExecuteSupportsFixedAndSpanArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileFunction(
                "function(a, b, c) return a + b + c end",
                funcFriendlyName: "compiled_add"
            );

            DynValue fixedResult = compiled.Execute(
                DynValue.FromNumber(1),
                DynValue.FromNumber(2),
                DynValue.FromNumber(3)
            );
            DynValue arrayResult = compiled.Execute(
                new[] { DynValue.FromNumber(4), DynValue.FromNumber(5), DynValue.FromNumber(6) }
            );
            DynValue spanResult = ExecuteCompiledScriptWithSpanArguments(
                compiled,
                new[] { DynValue.FromNumber(7), DynValue.FromNumber(8), DynValue.FromNumber(9) }
            );

            await Assert.That(fixedResult.Number).IsEqualTo(6d).ConfigureAwait(false);
            await Assert.That(arrayResult.Number).IsEqualTo(15d).ConfigureAwait(false);
            await Assert.That(spanResult.Number).IsEqualTo(24d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionExecuteSupportsFixedObjectArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileFunction(
                "function(...) return select('#', ...), ... end",
                funcFriendlyName: "compiled_capture"
            );

            DynValue result = compiled.Execute((object)null, "value", 42, true, 5d);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(6).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].String).IsEqualTo("value").ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(result.Tuple[4].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[5].Number).IsEqualTo(5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionExecuteSupportsSixAndSevenFixedArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileFunction(
                "function(...) return select('#', ...), ... end",
                funcFriendlyName: "compiled_wide_capture"
            );

            DynValue sixDynValueResult = compiled.Execute(
                DynValue.FromNumber(1),
                DynValue.FromNumber(2),
                DynValue.FromNumber(3),
                DynValue.FromNumber(4),
                DynValue.FromNumber(5),
                DynValue.FromNumber(6)
            );
            DynValue sevenDynValueResult = compiled.Execute(
                DynValue.FromNumber(1),
                DynValue.FromNumber(2),
                DynValue.FromNumber(3),
                DynValue.FromNumber(4),
                DynValue.FromNumber(5),
                DynValue.FromNumber(6),
                DynValue.FromNumber(7)
            );
            DynValue sixObjectResult = compiled.Execute(1d, 2d, 3d, 4d, 5d, 6d);
            DynValue sevenObjectResult = compiled.Execute(1d, 2d, 3d, 4d, 5d, 6d, 7d);

            await AssertCompiledCaptureResult(sixDynValueResult, 6).ConfigureAwait(false);
            await AssertCompiledCaptureResult(sevenDynValueResult, 7).ConfigureAwait(false);
            await AssertCompiledCaptureResult(sixObjectResult, 6).ConfigureAwait(false);
            await AssertCompiledCaptureResult(sevenObjectResult, 7).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionExecuteObjectArgumentsSupportsCallerOwnedSpan(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileFunction(
                "function(...) return select('#', ...), ... end",
                funcFriendlyName: "compiled_object_span_capture"
            );
            object[] args = { "padding", null, "value", 42, true, 5d, "tail", "padding" };

            DynValue result = compiled.ExecuteObjectArguments(args.AsSpan(1, 6));

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(7).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(6d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].String).IsEqualTo("value").ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(result.Tuple[4].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[5].Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(result.Tuple[6].String).IsEqualTo("tail").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionExecuteEmptySpanArgumentsUseZeroArgPath(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileFunction(
                """
                function(...)
                    local info = debug.getinfo(1, "f")
                    local identity = info.func == expected and "same" or "different"
                    return select("#", ...), identity
                end
                """,
                funcFriendlyName: "compiled_empty_span"
            );
            script.Globals.Set("expected", compiled.Function);

            DynValue spanResult = ExecuteCompiledScriptWithSpanArguments(
                compiled,
                Array.Empty<DynValue>()
            );
            DynValue objectSpanResult = ExecuteCompiledScriptWithObjectSpanArguments(
                compiled,
                Array.Empty<object>()
            );

            await Assert.That(spanResult.Tuple[0].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(spanResult.Tuple[1].String).IsEqualTo("same").ConfigureAwait(false);
            await Assert.That(objectSpanResult.Tuple[0].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert
                .That(objectSpanResult.Tuple[1].String)
                .IsEqualTo("same")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionExecuteApiShapeSupportsExplicitNullAndArrayForms(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileFunction(
                "function(...) return select('#', ...), ... end",
                funcFriendlyName: "compiled_api_shape"
            );
            DynValue[] args = new[] { DynValue.FromNumber(1), DynValue.FromNumber(2) };
            object[] objectArray = { "left", "right" };

            DynValue dynValueNilResult = compiled.Execute(DynValue.Nil);
            DynValue objectNullResult = compiled.Execute((object)null);
            DynValue arrayResult = compiled.Execute(args);
            DynValue spanResult = ExecuteCompiledScriptWithSpanArguments(compiled, args);
            DynValue objectArrayAsSingleResult = compiled.Execute((object)objectArray);
            DynValue objectArrayAsArgumentListResult = compiled.ExecuteObjectArguments(objectArray);

            await Assert
                .That(dynValueNilResult.Tuple[0].Number)
                .IsEqualTo(1d)
                .ConfigureAwait(false);
            await Assert
                .That(dynValueNilResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert.That(objectNullResult.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert
                .That(objectNullResult.Tuple[1].Type)
                .IsEqualTo(DataType.Nil)
                .ConfigureAwait(false);
            await Assert.That(arrayResult.Tuple[0].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(arrayResult.Tuple[1].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(arrayResult.Tuple[2].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(spanResult.Tuple[0].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(spanResult.Tuple[1].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(spanResult.Tuple[2].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert
                .That(objectArrayAsSingleResult.Tuple[0].Number)
                .IsEqualTo(1d)
                .ConfigureAwait(false);
            await Assert
                .That(objectArrayAsSingleResult.Tuple[1].Type)
                .IsEqualTo(DataType.Table)
                .ConfigureAwait(false);
            await Assert
                .That(objectArrayAsSingleResult.Tuple[1].Table.Get(1).String)
                .IsEqualTo("left")
                .ConfigureAwait(false);
            await Assert
                .That(objectArrayAsSingleResult.Tuple[1].Table.Get(2).String)
                .IsEqualTo("right")
                .ConfigureAwait(false);
            await Assert
                .That(objectArrayAsArgumentListResult.Tuple[0].Number)
                .IsEqualTo(2d)
                .ConfigureAwait(false);
            await Assert
                .That(objectArrayAsArgumentListResult.Tuple[1].String)
                .IsEqualTo("left")
                .ConfigureAwait(false);
            await Assert
                .That(objectArrayAsArgumentListResult.Tuple[2].String)
                .IsEqualTo("right")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionExecuteObjectArgumentsMatchScriptCall(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileFunction(
                "function(a, b, c) return a + b + c end",
                funcFriendlyName: "compiled_object_add"
            );

            DynValue executeResult = compiled.Execute(1d, 2d, 3d);
            DynValue callResult = script.Call(compiled.Function, 1d, 2d, 3d);

            await Assert.That(executeResult.Number).IsEqualTo(6d).ConfigureAwait(false);
            await Assert
                .That(executeResult.Number)
                .IsEqualTo(callResult.Number)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionExecutePreservesDebugFrameFunctionIdentity(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileFunction(
                """
                function()
                    local info = debug.getinfo(1, "fS")
                    local funcInfo = debug.getinfo(info.func, "S")
                    local identity = info.func == expected and "same" or "different"
                    return identity .. ":" .. type(info.func) .. ":" .. info.what .. ":" .. funcInfo.short_src
                end
                """,
                funcFriendlyName: "compiled_func_debug.lua"
            );
            script.Globals.Set("expected", compiled.Function);

            DynValue result = compiled.Execute();

            await Assert
                .That(result.String)
                .IsEqualTo("same:function:Lua:libfunc_compiled_func_debug.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CompileFunctionExecuteObjectArgumentsRejectsNullArray()
        {
            Script script = new(CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileFunction(
                "function(...) return select('#', ...) end",
                funcFriendlyName: "compiled_null_object_args"
            );

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                compiled.ExecuteObjectArguments((object[])null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionExecuteObjectArgumentRejectsForeignTable(
            LuaCompatibilityVersion version
        )
        {
            Script scriptA = new(version, CoreModulePresets.Complete);
            object foreignTable = scriptA.DoString("return {}");

            Script scriptB = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = scriptB.CompileFunction(
                "function(value) return value end",
                funcFriendlyName: "compiled_foreign_arg"
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                compiled.Execute(foreignTable)
            );
            ScriptRuntimeException spanException = Assert.Throws<ScriptRuntimeException>(() =>
                compiled.ExecuteObjectArguments(new[] { foreignTable })
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
            await Assert
                .That(spanException.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionExecuteRejectsForeignDynValueArguments(
            LuaCompatibilityVersion version
        )
        {
            Script scriptA = new(version, CoreModulePresets.Complete);
            DynValue foreignTable = scriptA.DoString("return {}");

            Script scriptB = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = scriptB.CompileFunction(
                "function(value) return value end",
                funcFriendlyName: "compiled_foreign_dynvalue_arg"
            );

            ScriptRuntimeException fixedException = Assert.Throws<ScriptRuntimeException>(() =>
                compiled.Execute(foreignTable)
            );
            ScriptRuntimeException arrayException = Assert.Throws<ScriptRuntimeException>(() =>
                compiled.Execute(new[] { foreignTable })
            );
            ScriptRuntimeException spanException = Assert.Throws<ScriptRuntimeException>(() =>
                ExecuteCompiledScriptWithSpanArguments(compiled, new[] { foreignTable })
            );

            await Assert
                .That(fixedException.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
            await Assert
                .That(arrayException.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
            await Assert
                .That(spanException.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaTestMatrix(1, 2, 3, 4, 5, 6, 7)]
        public async Task CompileFunctionExecuteRejectsForeignDynValueAtEachFixedArity(
            LuaCompatibilityVersion version,
            int arity
        )
        {
            Script scriptA = new(version, CoreModulePresets.Complete);
            DynValue foreignTable = scriptA.DoString("return {}");

            Script scriptB = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = scriptB.CompileFunction(
                "function(...) return select('#', ...) end",
                funcFriendlyName: "compiled_foreign_dynvalue_matrix"
            );

            for (int foreignIndex = 0; foreignIndex < arity; foreignIndex++)
            {
                DynValue[] args = CreateDynValueArguments(arity);
                args[foreignIndex] = foreignTable;

                ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                    ExecuteCompiledScriptWithFixedArguments(compiled, args)
                );

                await Assert
                    .That(exception.Message)
                    .Contains("different scripts")
                    .Because(
                        $"fixed DynValue arity {arity} should reject foreign argument index {foreignIndex}"
                    )
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        [LuaTestMatrix(1, 2, 3, 4, 5, 6, 7)]
        public async Task CompileFunctionExecuteRejectsForeignObjectAtEachFixedArity(
            LuaCompatibilityVersion version,
            int arity
        )
        {
            Script scriptA = new(version, CoreModulePresets.Complete);
            object foreignTable = scriptA.DoString("return {}");

            Script scriptB = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = scriptB.CompileFunction(
                "function(...) return select('#', ...) end",
                funcFriendlyName: "compiled_foreign_object_matrix"
            );

            for (int foreignIndex = 0; foreignIndex < arity; foreignIndex++)
            {
                object[] args = CreateObjectArguments(arity);
                args[foreignIndex] = foreignTable;

                ScriptRuntimeException fixedException = Assert.Throws<ScriptRuntimeException>(() =>
                    ExecuteCompiledScriptWithFixedObjectArguments(compiled, args)
                );
                ScriptRuntimeException spanException = Assert.Throws<ScriptRuntimeException>(() =>
                    compiled.ExecuteObjectArguments(args)
                );

                await Assert
                    .That(fixedException.Message)
                    .Contains("different scripts")
                    .Because(
                        $"fixed object arity {arity} should reject foreign argument index {foreignIndex}"
                    )
                    .ConfigureAwait(false);
                await Assert
                    .That(spanException.Message)
                    .Contains("different scripts")
                    .Because(
                        $"object span arity {arity} should reject foreign argument index {foreignIndex}"
                    )
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BindGlobalFunctionExecutesInitiallyResolvedGlobal(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString("function update(value) return value + 1 end");
            CompiledScript boundUpdate = script.BindGlobalFunction("update");

            script.DoString("function update(value) return value + 100 end");

            DynValue boundResult = boundUpdate.Execute(DynValue.FromNumber(10));
            DynValue currentGlobalResult = script.Call(
                script.Globals.Get("update"),
                DynValue.FromNumber(10)
            );

            await Assert.That(boundUpdate.IsValid).IsTrue().ConfigureAwait(false);
            await Assert.That(boundUpdate.Script).IsSameReferenceAs(script).ConfigureAwait(false);
            await Assert.That(boundResult.Number).IsEqualTo(11d).ConfigureAwait(false);
            await Assert.That(currentGlobalResult.Number).IsEqualTo(110d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BindGlobalFunctionNestedFixedPathsExecuteInitiallyResolvedGlobal(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                """
                api = {
                    update = function(value) return value + 1 end,
                    system = { update = function(value) return value + 2 end }
                }
                """
            );
            CompiledScript twoKey = script.BindGlobalFunction("api", "update");
            CompiledScript threeKey = script.BindGlobalFunction("api", "system", "update");

            script.DoString(
                """
                api.update = function(value) return value + 100 end
                api.system.update = function(value) return value + 200 end
                """
            );

            DynValue twoKeyResult = twoKey.Execute(DynValue.FromNumber(10));
            DynValue threeKeyResult = threeKey.Execute(DynValue.FromNumber(10));
            DynValue currentTwoKey = script.Call(
                script.Globals.Get("api", "update"),
                DynValue.FromNumber(10)
            );
            DynValue currentThreeKey = script.Call(
                script.Globals.Get("api", "system", "update"),
                DynValue.FromNumber(10)
            );

            await Assert.That(twoKeyResult.Number).IsEqualTo(11d).ConfigureAwait(false);
            await Assert.That(threeKeyResult.Number).IsEqualTo(12d).ConfigureAwait(false);
            await Assert.That(currentTwoKey.Number).IsEqualTo(110d).ConfigureAwait(false);
            await Assert.That(currentThreeKey.Number).IsEqualTo(210d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BindGlobalFunctionPathSupportsArraysSpansSlicesAndNumericKeys(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                """
                api = {
                    [1] = { run = function(value) return value + 3 end },
                    system = { tick = function(value) return value + 4 end }
                }
                """
            );
            object[] numericPath = new object[] { "api", 1, "run" };
            object[] systemPath = new object[] { "api", "system", "tick" };
            object[] paddedPath = new object[] { "ignored", "api", "system", "tick", "ignored" };

            CompiledScript arrayBound = script.BindGlobalFunctionPath(numericPath);
            CompiledScript spanBound = BindGlobalFunctionPathWithSpan(script, systemPath);
            CompiledScript sliceBound = BindGlobalFunctionPathWithSlice(script, paddedPath, 1, 3);

            DynValue arrayResult = arrayBound.Execute(DynValue.FromNumber(10));
            DynValue spanResult = spanBound.Execute(DynValue.FromNumber(10));
            DynValue sliceResult = sliceBound.Execute(DynValue.FromNumber(10));

            await Assert.That(arrayResult.Number).IsEqualTo(13d).ConfigureAwait(false);
            await Assert.That(spanResult.Number).IsEqualTo(14d).ConfigureAwait(false);
            await Assert.That(sliceResult.Number).IsEqualTo(14d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BindGlobalFunctionPathSupportsCallableTables(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                """
                api = {
                    callable = setmetatable({}, {
                        __call = function(_, value) return value * 2 end
                    })
                }
                """
            );

            CompiledScript boundCallable = script.BindGlobalFunction("api", "callable");
            DynValue result = boundCallable.Execute(DynValue.FromNumber(21));

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BindGlobalFunctionPathRejectsNullOrEmptyPath()
        {
            Script script = new(CoreModulePresets.Complete);

            ArgumentNullException nullException = Assert.Throws<ArgumentNullException>(() =>
                script.BindGlobalFunctionPath((object[])null)
            );
            ArgumentException emptyArrayException = Assert.Throws<ArgumentException>(() =>
                script.BindGlobalFunctionPath(Array.Empty<object>())
            );
            ArgumentException emptySpanException = Assert.Throws<ArgumentException>(() =>
                BindGlobalFunctionPathWithSpan(script, Array.Empty<object>())
            );

            await Assert.That(nullException.ParamName).IsEqualTo("keys").ConfigureAwait(false);
            await Assert
                .That(emptyArrayException.Message)
                .Contains("cannot be empty")
                .ConfigureAwait(false);
            await Assert
                .That(emptySpanException.Message)
                .Contains("cannot be empty")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BindGlobalFunctionNestedPathErrorsMatchTableLookup(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString("api = { leaf = 5 }");

            ArgumentException missingFinalException = Assert.Throws<ArgumentException>(() =>
                script.BindGlobalFunction("api", "missing")
            );
            ScriptRuntimeException missingIntermediateException =
                Assert.Throws<ScriptRuntimeException>(() =>
                    script.BindGlobalFunction("missing", "update")
                );
            ScriptRuntimeException nonTableIntermediateException =
                Assert.Throws<ScriptRuntimeException>(() =>
                    script.BindGlobalFunction("api", "leaf", "update")
                );

            await Assert
                .That(missingFinalException.Message)
                .Contains("__call metamethod")
                .ConfigureAwait(false);
            await Assert
                .That(missingIntermediateException.Message)
                .Contains("did not point to anything")
                .ConfigureAwait(false);
            await Assert
                .That(nonTableIntermediateException.Message)
                .Contains("did not point to a table")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BindFunctionSupportsCallableMetamethods(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callable = script.DoString(
                "local target = setmetatable({}, { __call = function(_, value) return value * 2 end }); return target"
            );

            CompiledScript boundCallable = script.BindFunction(callable);
            DynValue result = boundCallable.Execute(DynValue.FromNumber(21));

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BindFunctionReresolvesCallableMetamethodAfterBinding(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue values = script.DoString(
                """
                local target = {}
                local meta = { __call = function(_, value) return value + 1 end }
                setmetatable(target, meta)
                return target, meta
                """
            );
            DynValue callable = values.Tuple[0];
            Table meta = values.Tuple[1].Table;
            CompiledScript boundCallable = script.BindFunction(callable);

            DynValue first = boundCallable.Execute(DynValue.FromNumber(10));

            meta.Set(
                "__call",
                DynValue.NewCallback((_, args) => DynValue.FromNumber(args[1].Number + 20d))
            );
            DynValue second = boundCallable.Execute(DynValue.FromNumber(10));

            meta.Set("__call", DynValue.Nil);
            ArgumentException removedException = Assert.Throws<ArgumentException>(() =>
                boundCallable.Execute(DynValue.FromNumber(10))
            );

            await Assert.That(first.Number).IsEqualTo(11d).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(30d).ConfigureAwait(false);
            await Assert
                .That(removedException.Message)
                .Contains("__call metamethod")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task BindFunctionRejectsChainedCallMetamethodsBeforeLua54(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callable = CreateTableValuedCallTarget(script);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.BindFunction(callable)
            );

            await Assert.That(exception.Message).Contains("__call").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task BindFunctionFollowsChainedCallMetamethodsFromLua54(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue callable = CreateTableValuedCallTarget(script);
            CompiledScript boundCallable = script.BindFunction(callable);

            DynValue result = boundCallable.Execute(DynValue.FromNumber(7));

            await Assert.That(result.Number).IsEqualTo(12d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BindGlobalFunctionRejectsNonCallableGlobal(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.Globals.Set("notCallable", DynValue.FromNumber(42));

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.BindGlobalFunction("notCallable")
            );

            await Assert
                .That(exception.Message)
                .Contains("function is not a function")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BindFunctionRejectsForeignFunction(LuaCompatibilityVersion version)
        {
            Script scriptA = new(version, CoreModulePresets.Complete);
            DynValue foreignFunction = scriptA.DoString("return function() return 1 end");

            Script scriptB = new(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.BindFunction(foreignFunction)
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompiledScriptConstructorRejectsForeignFunction(
            LuaCompatibilityVersion version
        )
        {
            Script scriptA = new(version, CoreModulePresets.Complete);
            DynValue foreignFunction = scriptA.DoString("return function() return 1 end");

            Script scriptB = new(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                ConstructCompiledScript(scriptB, foreignFunction)
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompiledScriptConstructorRejectsNonCallableValue(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                ConstructCompiledScript(script, DynValue.FromNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("__call metamethod")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BindGlobalFunctionRejectsNullOrEmptyName()
        {
            Script script = new(CoreModulePresets.Complete);

            ArgumentException nullException = Assert.Throws<ArgumentException>(() =>
                script.BindGlobalFunction((string)null)
            );
            ArgumentException emptyException = Assert.Throws<ArgumentException>(() =>
                script.BindGlobalFunction(string.Empty)
            );

            await Assert.That(nullException.ParamName).IsEqualTo("name").ConfigureAwait(false);
            await Assert.That(emptyException.ParamName).IsEqualTo("name").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileStringRuntimeErrorUsesFriendlyName(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileString(
                "local f = nil; return f()",
                codeFriendlyName: "compiled_error.lua"
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                compiled.Execute()
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("compiled_error.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileFunctionRuntimeErrorUsesFriendlyNameOnFastArgumentPaths(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            CompiledScript compiled = script.CompileFunction(
                "function(value) local missing = nil; return missing(value) end",
                funcFriendlyName: "compiled_fast_arg_error"
            );

            ScriptRuntimeException fixedException = Assert.Throws<ScriptRuntimeException>(() =>
                compiled.Execute(DynValue.FromNumber(1))
            );
            ScriptRuntimeException arrayException = Assert.Throws<ScriptRuntimeException>(() =>
                compiled.Execute(new[] { DynValue.FromNumber(2) })
            );
            ScriptRuntimeException spanException = Assert.Throws<ScriptRuntimeException>(() =>
                ExecuteCompiledScriptWithSpanArguments(
                    compiled,
                    new[] { DynValue.FromNumber(3), DynValue.FromNumber(4) }
                )
            );

            await Assert
                .That(fixedException.DecoratedMessage)
                .Contains("compiled_fast_arg_error")
                .ConfigureAwait(false);
            await Assert
                .That(arrayException.DecoratedMessage)
                .Contains("compiled_fast_arg_error")
                .ConfigureAwait(false);
            await Assert
                .That(spanException.DecoratedMessage)
                .Contains("compiled_fast_arg_error")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CompileStringExecutionDoesNotSignalDebuggerAgain(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            RecordingDebugger debugger = new();
            script.AttachDebugger(debugger);

            CompiledScript compiled = script.CompileString(
                "return 42",
                codeFriendlyName: "compiled_debug.lua"
            );
            int sourceNotifications = debugger.SourceCodeSetCount;
            int byteCodeNotifications = debugger.ByteCodeSetCount;

            compiled.Execute();
            compiled.Execute();

            await Assert
                .That(debugger.SourceCodeSetCount)
                .IsEqualTo(sourceNotifications)
                .ConfigureAwait(false);
            await Assert
                .That(debugger.ByteCodeSetCount)
                .IsEqualTo(byteCodeNotifications)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultCompiledScriptReportsInvalidHandle()
        {
            CompiledScript compiled = default;

            await Assert.That(compiled.IsValid).IsFalse().ConfigureAwait(false);
            await Assert
                .That(Assert.Throws<InvalidOperationException>(() => compiled.Execute()).Message)
                .Contains("Script compile or function binding method")
                .ConfigureAwait(false);
        }

        private static DynValue ExecuteCompiledScriptWithSpanArguments(
            CompiledScript compiled,
            DynValue[] args
        )
        {
            return compiled.Execute(args.AsSpan());
        }

        private static DynValue ExecuteCompiledScriptWithObjectSpanArguments(
            CompiledScript compiled,
            object[] args
        )
        {
            return compiled.ExecuteObjectArguments(args.AsSpan());
        }

        private static CompiledScript BindGlobalFunctionPathWithSpan(Script script, object[] keys)
        {
            return script.BindGlobalFunctionPath(keys.AsSpan());
        }

        private static CompiledScript BindGlobalFunctionPathWithSlice(
            Script script,
            object[] keys,
            int start,
            int length
        )
        {
            return script.BindGlobalFunctionPath(keys.AsSpan(start, length));
        }

        private static async Task AssertCompiledCaptureResult(DynValue result, int arity)
        {
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(arity + 1).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo((double)arity)
                .ConfigureAwait(false);
            for (int i = 0; i < arity; i++)
            {
                await Assert
                    .That(result.Tuple[i + 1].Number)
                    .IsEqualTo(i + 1d)
                    .ConfigureAwait(false);
            }
        }

        private static void ConstructCompiledScript(Script script, DynValue function)
        {
            CompiledScript compiled = new(script, function);
            _ = compiled.IsValid;
        }

        private static DynValue ExecuteCompiledScriptWithFixedArguments(
            CompiledScript compiled,
            DynValue[] args
        )
        {
            return args.Length switch
            {
                1 => compiled.Execute(args[0]),
                2 => compiled.Execute(args[0], args[1]),
                3 => compiled.Execute(args[0], args[1], args[2]),
                4 => compiled.Execute(args[0], args[1], args[2], args[3]),
                5 => compiled.Execute(args[0], args[1], args[2], args[3], args[4]),
                6 => compiled.Execute(args[0], args[1], args[2], args[3], args[4], args[5]),
                7 => compiled.Execute(
                    args[0],
                    args[1],
                    args[2],
                    args[3],
                    args[4],
                    args[5],
                    args[6]
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(args)),
            };
        }

        private static DynValue ExecuteCompiledScriptWithFixedObjectArguments(
            CompiledScript compiled,
            object[] args
        )
        {
            return args.Length switch
            {
                1 => compiled.Execute(args[0]),
                2 => compiled.Execute(args[0], args[1]),
                3 => compiled.Execute(args[0], args[1], args[2]),
                4 => compiled.Execute(args[0], args[1], args[2], args[3]),
                5 => compiled.Execute(args[0], args[1], args[2], args[3], args[4]),
                6 => compiled.Execute(args[0], args[1], args[2], args[3], args[4], args[5]),
                7 => compiled.Execute(
                    args[0],
                    args[1],
                    args[2],
                    args[3],
                    args[4],
                    args[5],
                    args[6]
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(args)),
            };
        }

        private static DynValue[] CreateDynValueArguments(int count)
        {
            DynValue[] args = new DynValue[count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = DynValue.FromNumber(i + 1);
            }

            return args;
        }

        private static object[] CreateObjectArguments(int count)
        {
            object[] args = new object[count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = i + 1d;
            }

            return args;
        }

        private static DynValue CreateTableValuedCallTarget(Script script)
        {
            return script.DoString(
                """
                local target = {}
                local proxy = {}
                setmetatable(target, { __call = proxy })
                setmetatable(proxy, {
                    __call = function(proxySelf, targetSelf, value)
                        if proxySelf ~= proxy or targetSelf ~= target then
                            return -1
                        end

                        return value + 5
                    end
                })
                return target
                """
            );
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DoStringCacheHitPreservesDebugInfoShape(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            const string code = """
                local info = debug.getinfo(1, "fS")
                local funcInfo = debug.getinfo(info.func, "S")
                return type(info.func) .. ":" .. info.what .. ":" .. funcInfo.what .. ":" .. info.short_src .. ":" .. funcInfo.short_src
                """;

            DynValue result1 = script.DoString(code, codeFriendlyName: "debug-info.lua");
            DynValue result2 = script.DoString(code, codeFriendlyName: "debug-info.lua");

            await Assert
                .That(result1.String)
                .IsEqualTo("function:Lua:Lua:debug-info.lua:debug-info.lua")
                .ConfigureAwait(false);
            await Assert.That(result1.String).IsEqualTo(result2.String).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua51)]
        public async Task DoStringCacheHitUsesFreshLua51SetfenvEnvironment(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModulePresets.Complete, options);
            script.Globals.Set("marker", DynValue.FromNumber(5));
            string code = string.Join(
                "\n",
                "local before = getfenv(1).marker",
                "setfenv(1, { marker = 99, setfenv = setfenv })",
                "return before * 100 + getfenv(1).marker"
            );

            DynValue result1 = script.DoString(code, codeFriendlyName: "setfenv-cache.lua");
            DynValue result2 = script.DoString(code, codeFriendlyName: "setfenv-cache.lua");

            await Assert.That(result1.Number).IsEqualTo(599d).ConfigureAwait(false);
            await Assert.That(result2.Number).IsEqualTo(599d).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CachedNamedLoadStringRuntimeErrorUsesFriendlyName(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            const string code = "local f = nil; return f()";

            script.LoadString(code, codeFriendlyName: "runtime_named.lua");
            DynValue cachedChunk = script.LoadString(code, codeFriendlyName: "runtime_named.lua");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(cachedChunk)
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("runtime_named.lua")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CachedNamedDoStringRuntimeErrorUsesFriendlyName(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            const string code = "local f = nil; return f()";

            Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(code, codeFriendlyName: "runtime_do_named.lua")
            );
            ScriptRuntimeException cachedException = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(code, codeFriendlyName: "runtime_do_named.lua")
            );
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);

            DynValue recoveryResult = script.DoString("return 1", codeFriendlyName: "recovery.lua");

            await Assert
                .That(cachedException.DecoratedMessage)
                .Contains("runtime_do_named.lua")
                .ConfigureAwait(false);
            await Assert.That(recoveryResult.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringBase64DumpDoesNotEnterCompilationCache(
            LuaCompatibilityVersion version
        )
        {
            Script producer = new(version, CoreModulePresets.Complete);
            DynValue chunk = producer.LoadString("return 127");
            using MemoryStream dump = new();
            producer.Dump(chunk, dump);
            string encodedDump =
                StringModule.Base64DumpHeader + Convert.ToBase64String(dump.ToArray());

            Script consumer = new(version, CoreModulePresets.Complete);
            DynValue result1 = consumer.LoadString(encodedDump, codeFriendlyName: "dump.lua");
            DynValue result2 = consumer.LoadString(encodedDump, codeFriendlyName: "dump.lua");

            await Assert.That(consumer.Call(result1).Number).IsEqualTo(127d).ConfigureAwait(false);
            await Assert.That(consumer.Call(result2).Number).IsEqualTo(127d).ConfigureAwait(false);
            await Assert.That(consumer.CompilationCacheCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileWithNullReturnThrows(LuaCompatibilityVersion version)
        {
            NullReturningScriptLoader loader = new();
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            InvalidCastException exception = Assert.Throws<InvalidCastException>(() =>
                script.LoadFile("test.lua")
            );

            await Assert.That(exception.Message).Contains("null").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileWithUnsupportedTypeThrows(LuaCompatibilityVersion version)
        {
            UnsupportedTypeScriptLoader loader = new();
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            InvalidCastException exception = Assert.Throws<InvalidCastException>(() =>
                script.LoadFile("test.lua")
            );

            await Assert
                .That(exception.Message)
                .Contains("Unsupported return type")
                .ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("Int32").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DumpWithNullFunctionThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            using MemoryStream ms = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.Dump(null, ms)
            );

            await Assert.That(exception.ParamName).IsEqualTo("function").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DumpWithNullStreamThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue func = script.LoadString("return 1");

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.Dump(func, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("stream").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DumpWithNonFunctionThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            using MemoryStream ms = new();
            DynValue notFunction = DynValue.NewNumber(42);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Dump(notFunction, ms)
            );

            await Assert.That(exception.Message).Contains("not a function").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DumpWithReadOnlyStreamThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue func = script.LoadString("return 1");
            using ReadOnlyMemoryStream roStream = new();

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Dump(func, roStream)
            );

            await Assert.That(exception.Message).Contains("readonly").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DumpWithClosureUpvaluesThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local captured = 10
                withCapture = function() return captured end
            "
            );
            DynValue func = script.Globals.Get("withCapture");
            using MemoryStream ms = new();

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Dump(func, ms)
            );

            await Assert.That(exception.Message).Contains("upvalues").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetTypeMetatableWithInvalidTypeThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            Table metatable = new(script);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.SetTypeMetatable((DataType)999, metatable)
            );

            await Assert.That(exception.Message).Contains("not supported").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetTypeMetatableWithInvalidTypeReturnsNull(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);

            Table result = script.GetTypeMetatable((DataType)999);

            await Assert.That(result).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AttachDebuggerWithNullThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.AttachDebugger(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("debugger").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AttachDebuggerSignalsSourceAndByteCode(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            int initialSourceCount = script.SourceCodeCount;
            script.LoadString("return 1", codeFriendlyName: "chunk1");
            script.LoadString("return 2", codeFriendlyName: "chunk2");

            RecordingDebugger debugger = new();
            script.AttachDebugger(debugger);

            // Debugger receives all sources loaded so far (initial + 2 we added)
            await Assert
                .That(debugger.SourceCodeSetCount)
                .IsEqualTo(initialSourceCount + 2)
                .ConfigureAwait(false);
            await Assert.That(debugger.ByteCodeSetCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(script.DebuggerEnabled).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringSignalsByteCodeChangeWhenDebuggerAttached(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            RecordingDebugger debugger = new();
            script.AttachDebugger(debugger);

            int initialCount = debugger.ByteCodeSetCount;

            script.LoadString("return 42");

            await Assert
                .That(debugger.ByteCodeSetCount)
                .IsGreaterThan(initialCount)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringCacheHitDoesNotSignalDebuggerAgain(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            RecordingDebugger debugger = new();
            script.AttachDebugger(debugger);

            script.LoadString("return 42");
            int sourceNotifications = debugger.SourceCodeSetCount;
            int byteCodeNotifications = debugger.ByteCodeSetCount;
            int sourceCodeCount = script.SourceCodeCount;

            script.LoadString("return 42");

            await Assert
                .That(debugger.SourceCodeSetCount)
                .IsEqualTo(sourceNotifications)
                .ConfigureAwait(false);
            await Assert
                .That(debugger.ByteCodeSetCount)
                .IsEqualTo(byteCodeNotifications)
                .ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(sourceCodeCount)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringNamedCacheHitDoesNotSignalDebuggerAgain(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            RecordingDebugger debugger = new();
            script.AttachDebugger(debugger);

            script.LoadString("return 42", codeFriendlyName: "named");
            int sourceNotifications = debugger.SourceCodeSetCount;
            int byteCodeNotifications = debugger.ByteCodeSetCount;
            int sourceCodeCount = script.SourceCodeCount;

            script.LoadString("return 42", codeFriendlyName: "named");

            await Assert
                .That(debugger.SourceCodeSetCount)
                .IsEqualTo(sourceNotifications)
                .ConfigureAwait(false);
            await Assert
                .That(debugger.ByteCodeSetCount)
                .IsEqualTo(byteCodeNotifications)
                .ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(sourceCodeCount)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringDifferentFriendlyNameSignalsDebugger(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            RecordingDebugger debugger = new();
            script.AttachDebugger(debugger);

            script.LoadString("return 42", codeFriendlyName: "first");
            int sourceNotifications = debugger.SourceCodeSetCount;
            int byteCodeNotifications = debugger.ByteCodeSetCount;
            int sourceCodeCount = script.SourceCodeCount;

            script.LoadString("return 42", codeFriendlyName: "second");

            await Assert
                .That(debugger.SourceCodeSetCount)
                .IsEqualTo(sourceNotifications + 1)
                .ConfigureAwait(false);
            await Assert
                .That(debugger.ByteCodeSetCount)
                .IsEqualTo(byteCodeNotifications + 1)
                .ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(sourceCodeCount + 1)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateDynamicExpressionRemovesSourceOnError(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            int beforeCount = script.SourceCodeCount;

            Assert.Throws<SyntaxErrorException>(() => script.CreateDynamicExpression("+++"));

            await Assert.That(script.SourceCodeCount).IsEqualTo(beforeCount).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateConstantDynamicExpressionValidatesOwnership(
            LuaCompatibilityVersion version
        )
        {
            Script scriptA = new(CoreModulePresets.Complete);
            Script scriptB = new(CoreModulePresets.Complete);
            DynValue foreignValue = scriptA.DoString("return {}");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.CreateConstantDynamicExpression("constant", foreignValue)
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WarmUpInitializesLexerParser(LuaCompatibilityVersion version)
        {
            Script.WarmUp();
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetBannerIncludesVersion(LuaCompatibilityVersion version)
        {
            string banner = Script.GetBanner();

            await Assert.That(banner).Contains("NovaSharp").ConfigureAwait(false);
            await Assert.That(banner).Contains(Script.VERSION).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetBannerWithSubproductIncludesSubproduct(LuaCompatibilityVersion version)
        {
            string banner = Script.GetBanner("TestProduct");

            await Assert.That(banner).Contains("TestProduct").ConfigureAwait(false);
            await Assert.That(banner).Contains("NovaSharp").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OwnerScriptReturnsSelf(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);

            await Assert.That(script.OwnerScript).IsSameReferenceAs(script).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetSourceCodeReturnsCorrectSource(LuaCompatibilityVersion version)
        {
            Script script = new(CoreModules.Basic);
            int initialSourceCount = script.SourceCodeCount;
            script.LoadString("return 1", codeFriendlyName: "source_one");
            script.LoadString("return 2", codeFriendlyName: "source_two");

            SourceCode source0 = script.GetSourceCode(initialSourceCount);
            SourceCode source1 = script.GetSourceCode(initialSourceCount + 1);

            await Assert.That(source0.Name).IsEqualTo("source_one").ConfigureAwait(false);
            await Assert.That(source1.Name).IsEqualTo("source_two").ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 2)
                .ConfigureAwait(false);
        }

        // Helper classes for testing

        private sealed class ByteArrayScriptLoader : ScriptLoaderBase
        {
            private readonly byte[] _bytes;

            public ByteArrayScriptLoader(byte[] bytes)
            {
                _bytes = bytes;
            }

            public override object LoadFile(string file, Table globalContext) => _bytes;

            public override bool ScriptFileExists(string name) => true;
        }

        private sealed class StreamScriptLoader : ScriptLoaderBase
        {
            private readonly string _code;

            public StreamScriptLoader(string code)
            {
                _code = code;
            }

            public override object LoadFile(string file, Table globalContext)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(_code);
                return new MemoryStream(bytes);
            }

            public override bool ScriptFileExists(string name) => true;
        }

        private sealed class TrackingStreamScriptLoader : ScriptLoaderBase
        {
            private readonly string _code;

            public TrackingStreamScriptLoader(string code)
            {
                _code = code;
            }

            public int LoadCount { get; private set; }

            public TrackingMemoryStream LastStream { get; private set; }

            public override object LoadFile(string file, Table globalContext)
            {
                LoadCount++;
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(_code);
                LastStream = new TrackingMemoryStream(bytes);
                return LastStream;
            }

            public override bool ScriptFileExists(string name) => true;
        }

        private sealed class CountingStringScriptLoader : ScriptLoaderBase
        {
            private readonly string _code;

            public CountingStringScriptLoader(string code)
            {
                _code = code;
            }

            public int LoadCount { get; private set; }

            public override object LoadFile(string file, Table globalContext)
            {
                LoadCount++;
                return _code;
            }

            public override bool ScriptFileExists(string name) => true;
        }

        private sealed class NullReturningScriptLoader : ScriptLoaderBase
        {
            public override object LoadFile(string file, Table globalContext) => null;

            public override bool ScriptFileExists(string name) => true;
        }

        private sealed class UnsupportedTypeScriptLoader : ScriptLoaderBase
        {
            public override object LoadFile(string file, Table globalContext) => 123;

            public override bool ScriptFileExists(string name) => true;
        }

        private sealed class ReadOnlyMemoryStream : MemoryStream
        {
            public override bool CanWrite => false;
        }

        private sealed class TrackingMemoryStream : MemoryStream
        {
            public TrackingMemoryStream(byte[] buffer)
                : base(buffer) { }

            public bool IsDisposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }

        private sealed class RecordingDebugger : IDebugger
        {
            public int SourceCodeSetCount { get; private set; }
            public int ByteCodeSetCount { get; private set; }

            public void SetSourceCode(SourceCode sourceCode)
            {
                SourceCodeSetCount++;
            }

            public void SetByteCode(string[] byteCode)
            {
                ByteCodeSetCount++;
            }

            public DebuggerCaps GetDebuggerCaps() => DebuggerCaps.CanDebugSourceCode;

            public void SetDebugService(DebugService debugService) { }

            public bool IsPauseRequested() => false;

            public DebuggerAction GetAction(int instructionPointer, SourceRef sourceRef) =>
                new() { Action = DebuggerAction.ActionType.Run };

            public void SignalExecutionEnded() { }

            public void Update(WatchType watchType, IEnumerable<WatchItem> watchItems) { }

            public IReadOnlyList<DynamicExpression> GetWatchItems() =>
                Array.Empty<DynamicExpression>();

            public bool SignalRuntimeException(ScriptRuntimeException ex) => false;

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }
        }
    }
}
