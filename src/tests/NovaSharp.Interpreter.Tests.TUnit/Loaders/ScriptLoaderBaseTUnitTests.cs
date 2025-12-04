namespace NovaSharp.Interpreter.Tests.TUnit.Loaders
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;

    public sealed class ScriptLoaderBaseTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task UnpackStringPathsSplitsAndTrimsSegments()
        {
            IReadOnlyList<string> segments = ScriptLoaderBase.UnpackStringPaths(
                " ? ;?.lua ; ./?.txt ;  "
            );
            List<string> expected = new() { "?", "?.lua", "./?.txt" };

            await Assert.That(string.Join(",", segments)).IsEqualTo(string.Join(",", expected));
        }

        [global::TUnit.Core.Test]
        public async Task UnpackStringPathsThrowsWhenNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                ScriptLoaderBase.UnpackStringPaths(null);
            });

            await Assert.That(exception.ParamName).IsEqualTo("str");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameFromPathsReturnsFirstMatchingFile()
        {
            TestScriptLoader loader = new();
            loader.AddExisting("modules/foo.lua");

            string resolved = loader.ResolveFromPaths("foo", "modules/?.lua", "fallback/?.lua");

            await Assert.That(resolved).IsEqualTo("modules/foo.lua");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameReturnsNullWhenNoPathsMatch()
        {
            TestScriptLoader loader = new();

            string resolved = loader.ResolveFromPaths("missing", Array.Empty<string>());

            await Assert.That(resolved).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameThrowsWhenModuleNameNull()
        {
            Script script = new();
            TestScriptLoader loader = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                loader.ResolveModuleName(null, script.Globals);
            });

            await Assert.That(exception.ParamName).IsEqualTo("modname");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameUsesModulePathsWhenLuaPathIgnored()
        {
            Script script = new();
            TestScriptLoader loader = new()
            {
                IgnoreLuaPathGlobal = true,
                ModulePaths = new[] { "mods/?.lua" },
            };
            loader.AddExisting("mods/sample.lua");

            string resolved = loader.ResolveModuleName("sample", script.Globals);

            await Assert.That(resolved).IsEqualTo("mods/sample.lua");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameUsesLuaPathGlobalWhenAllowed()
        {
            Script script = new();
            script.Globals.Set("LUA_PATH", DynValue.NewString("lib/?.lua"));

            TestScriptLoader loader = new();
            loader.AddExisting("lib/widget.lua");

            string resolved = loader.ResolveModuleName("widget", script.Globals);

            await Assert.That(resolved).IsEqualTo("lib/widget.lua");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameThrowsWhenGlobalContextNull()
        {
            TestScriptLoader loader = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                loader.ResolveModuleName("module", null);
            });

            await Assert.That(exception.ParamName).IsEqualTo("globalContext");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameReturnsNullWhenPathListNull()
        {
            TestScriptLoader loader = new();

            string resolved = loader.ResolveFromPaths("foo", null);

            await Assert.That(resolved).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task ResolveFileNameTrimsWhitespace()
        {
            Script script = new();
            TestScriptLoader loader = new();

            string resolved = loader.ResolveFileName("  foo.lua  ", script.Globals);

            await Assert.That(resolved).IsEqualTo("foo.lua");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameNormalizesDottedModules()
        {
            TestScriptLoader loader = new();
            loader.AddExisting("modules/foo/bar.lua");

            string resolved = loader.ResolveFromPaths("foo.bar", "modules/?.lua");

            await Assert.That(resolved).IsEqualTo("modules/foo/bar.lua");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveFileNameReturnsOriginalWhenNoWhitespace()
        {
            Script script = new();
            TestScriptLoader loader = new();

            string original = "foo.lua";
            string resolved = loader.ResolveFileName(original, script.Globals);

            // Should return the exact same string instance when no trimming needed
            await Assert.That(object.ReferenceEquals(resolved, original)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ResolveFileNameThrowsWhenFilenameNull()
        {
            Script script = new();
            TestScriptLoader loader = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                loader.ResolveFileName(null, script.Globals);
            });

            await Assert.That(exception.ParamName).IsEqualTo("filename");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameProtectedThrowsWhenModuleNameNull()
        {
            TestScriptLoader loader = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                loader.ResolveFromPaths(null, "some/path");
            });

            await Assert.That(exception.ParamName).IsEqualTo("modname");
        }

        [global::TUnit.Core.Test]
        public async Task GetDefaultEnvironmentPathsReturnsFallbackWhenNoEnvVarsSet()
        {
            // When neither NOVASHARP_PATH nor LUA_PATH is set, should return default "?;?.lua"
            IReadOnlyList<string> paths = ScriptLoaderBase.GetDefaultEnvironmentPaths();

            await Assert.That(paths).IsNotNull();
            // The exact paths depend on environment, but should not be empty
            await Assert.That(paths.Count).IsGreaterThan(0);
        }

        [global::TUnit.Core.Test]
        public async Task UnpackStringPathsReturnsEmptyForWhitespaceOnlyString()
        {
            IReadOnlyList<string> segments = ScriptLoaderBase.UnpackStringPaths("   ;  ;   ");

            await Assert.That(segments.Count).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task UnpackStringPathsReturnsSinglePathWithoutSeparator()
        {
            IReadOnlyList<string> segments = ScriptLoaderBase.UnpackStringPaths("single/path");

            await Assert.That(segments.Count).IsEqualTo(1);
            await Assert.That(segments[0]).IsEqualTo("single/path");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameFallsBackToModulePathsWhenLuaPathNotString()
        {
            Script script = new();
            script.Globals.Set("LUA_PATH", DynValue.NewNumber(42)); // Not a string

            TestScriptLoader loader = new() { ModulePaths = new[] { "fallback/?.lua" } };
            loader.AddExisting("fallback/test.lua");

            string resolved = loader.ResolveModuleName("test", script.Globals);

            await Assert.That(resolved).IsEqualTo("fallback/test.lua");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameFallsBackToModulePathsWhenLuaPathNull()
        {
            Script script = new();
            // LUA_PATH not set at all

            TestScriptLoader loader = new() { ModulePaths = new[] { "mods/?.lua" } };
            loader.AddExisting("mods/mymod.lua");

            string resolved = loader.ResolveModuleName("mymod", script.Globals);

            await Assert.That(resolved).IsEqualTo("mods/mymod.lua");
        }

        private sealed class TestScriptLoader : ScriptLoaderBase
        {
            private readonly HashSet<string> _existing = new(StringComparer.Ordinal);

            public override bool ScriptFileExists(string name)
            {
                return _existing.Contains(name);
            }

            public override object LoadFile(string file, Table globalContext)
            {
                throw new NotImplementedException();
            }

            public void AddExisting(string path)
            {
                _existing.Add(path);
            }

            public string ResolveFromPaths(string moduleName, params string[] paths)
            {
                return ResolveModuleName(moduleName, paths);
            }
        }
    }
}
