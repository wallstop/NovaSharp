namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptLoaderBaseTests
    {
        private static readonly string[] SingleModuleProbe = { "modules/my/module.lua" };
        private static readonly string[] SampleModulePaths = { "libs/?.lua", "addons/?/init.lua" };
        private static readonly string[] SampleResolvedProbes =
        {
            "libs/sample.lua",
            "addons/sample/init.lua",
        };
        private static readonly string[] TrimmedPathSegments = { "?", "lib/?.lua", "scripts/?" };
        private static readonly string[] NovaSharpEnvironmentPaths =
        {
            "mods/?.lua",
            "packages/?/init.lua",
        };
        private static readonly string[] LuaPathEnvironmentPaths =
        {
            "lua/?.lua",
            "lua/?/init.lua",
        };
        private static readonly string[] DefaultFallbackPaths = { "?", "?.lua" };

        [Test]
        public void ResolveModuleNameUsesLuaPathGlobal()
        {
            Script script = new Script();
            Table globals = script.Globals;
            globals.Set("LUA_PATH", DynValue.NewString("modules/?.lua;legacy/?.lua"));

            TestScriptLoader loader = new();
            loader.ExistingFiles.Add("modules/my/module.lua");

            string resolved = loader.ResolveModuleName("my.module", globals);

            Assert.Multiple(() =>
            {
                Assert.That(resolved, Is.EqualTo("modules/my/module.lua"));
                Assert.That(loader.ProbedPaths, Is.EqualTo(SingleModuleProbe));
            });
        }

        [Test]
        public void ResolveModuleNameRespectsIgnoreLuaPathGlobal()
        {
            Script script = new Script();
            Table globals = script.Globals;
            globals.Set("LUA_PATH", DynValue.NewString("ignored/?.lua"));

            TestScriptLoader loader = new()
            {
                IgnoreLuaPathGlobal = true,
                ModulePaths = SampleModulePaths,
            };

            loader.ExistingFiles.Add("addons/sample/init.lua");

            string resolved = loader.ResolveModuleName("sample", globals);

            Assert.Multiple(() =>
            {
                Assert.That(resolved, Is.EqualTo("addons/sample/init.lua"));
                Assert.That(loader.ProbedPaths, Is.EqualTo(SampleResolvedProbes));
            });
        }

        [Test]
        public void ResolveModuleNameReturnsNullWhenNoMatches()
        {
            Script script = new Script();
            Table globals = script.Globals;
            TestScriptLoader loader = new();

            string resolved = loader.ResolveModuleName("missing", globals);

            Assert.Multiple(() =>
            {
                Assert.That(resolved, Is.Null);
                Assert.That(loader.ProbedPaths, Is.Empty);
            });
        }

        [Test]
        public void ResolveModuleNameThrowsWhenModuleNameIsNull()
        {
            Script script = new Script();
            Table globals = script.Globals;
            TestScriptLoader loader = new();

            Assert.That(
                () => loader.ResolveModuleName(null!, globals),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("modname")
            );
        }

        [Test]
        public void ResolveModuleNameThrowsWhenGlobalsAreNull()
        {
            TestScriptLoader loader = new();

            Assert.That(
                () => loader.ResolveModuleName("module", null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("globalContext")
            );
        }

        [Test]
        public void ResolveModuleNamePathsOverloadThrowsWhenModuleNameIsNull()
        {
            TestScriptLoader loader = new();

            Assert.That(
                () => loader.ResolveModuleNameWithPaths(null!, Array.Empty<string>()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("modname")
            );
        }

        [Test]
        public void UnpackStringPathsTrimsAndSkipsEmptySegments()
        {
            IReadOnlyList<string> paths = ScriptLoaderBase.UnpackStringPaths(
                " ? ; ; lib/?.lua ; scripts/? ;"
            );

            Assert.That(paths, Is.EqualTo(TrimmedPathSegments));
        }

        [Test]
        public void UnpackStringPathsThrowsWhenInputIsNull()
        {
            Assert.That(
                () => ScriptLoaderBase.UnpackStringPaths(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("str")
            );
        }

        [Test]
        public void GetDefaultEnvironmentPathsPrefersNovaSharpPath()
        {
            IPlatformAccessor original = Script.GlobalOptions.Platform;
            LoaderPlatformStub stub = new();
            stub.Environment["NOVASHARP_PATH"] = "mods/?.lua;packages/?/init.lua";

            Script.GlobalOptions.Platform = stub;

            try
            {
                IReadOnlyList<string> paths = ScriptLoaderBase.GetDefaultEnvironmentPaths();
                Assert.That(paths, Is.EqualTo(NovaSharpEnvironmentPaths));
            }
            finally
            {
                Script.GlobalOptions.Platform = original;
            }
        }

        [Test]
        public void GetDefaultEnvironmentPathsFallsBackToLuaPath()
        {
            IPlatformAccessor original = Script.GlobalOptions.Platform;
            LoaderPlatformStub stub = new();
            stub.Environment["LUA_PATH"] = "lua/?.lua;lua/?/init.lua";

            Script.GlobalOptions.Platform = stub;

            try
            {
                IReadOnlyList<string> paths = ScriptLoaderBase.GetDefaultEnvironmentPaths();
                Assert.That(paths, Is.EqualTo(LuaPathEnvironmentPaths));
            }
            finally
            {
                Script.GlobalOptions.Platform = original;
            }
        }

        [Test]
        public void GetDefaultEnvironmentPathsUsesFallbackWhenEnvironmentMissing()
        {
            IPlatformAccessor original = Script.GlobalOptions.Platform;
            LoaderPlatformStub stub = new();

            Script.GlobalOptions.Platform = stub;

            try
            {
                IReadOnlyList<string> paths = ScriptLoaderBase.GetDefaultEnvironmentPaths();
                Assert.That(paths, Is.EqualTo(DefaultFallbackPaths));
            }
            finally
            {
                Script.GlobalOptions.Platform = original;
            }
        }

        private sealed class TestScriptLoader : ScriptLoaderBase
        {
            public HashSet<string> ExistingFiles { get; } = new(StringComparer.Ordinal);

            public List<string> ProbedPaths { get; } = new();

            public override bool ScriptFileExists(string name)
            {
                ProbedPaths.Add(name);
                return ExistingFiles.Contains(name);
            }

            public override object LoadFile(string file, Table globalContext)
            {
                throw new NotSupportedException();
            }

            public string ResolveModuleNameWithPaths(string modname, IEnumerable<string> paths) =>
                base.ResolveModuleName(modname, paths);
        }

        private sealed class LoaderPlatformStub : IPlatformAccessor
        {
            public Dictionary<string, string> Environment { get; } = new(StringComparer.Ordinal);

            public CoreModules FilterSupportedCoreModules(CoreModules coreModules) => coreModules;

            public string GetEnvironmentVariable(string envvarname) =>
                Environment.TryGetValue(envvarname, out string value) ? value : null;

            public bool IsRunningOnAOT() => false;

            public string GetPlatformName() => "LoaderPlatformStub";

            public void DefaultPrint(string content) { }

            public string DefaultInput(string prompt) => string.Empty;

            public Stream OpenFile(
                Script script,
                string filename,
                Encoding encoding,
                string mode
            ) => Stream.Null;

            public Stream GetStandardStream(StandardFileType type) => Stream.Null;

            public string GetTempFileName() => "temp";

            public void ExitFast(int exitCode) => throw new NotSupportedException();

            public bool FileExists(string file) => false;

            public void DeleteFile(string file) => throw new NotSupportedException();

            public void MoveFile(string src, string dst) => throw new NotSupportedException();

            public int ExecuteCommand(string cmdline) => throw new NotSupportedException();
        }
    }
}
