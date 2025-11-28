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
