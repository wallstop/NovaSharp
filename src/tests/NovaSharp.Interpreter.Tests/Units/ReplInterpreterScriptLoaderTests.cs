namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.REPL;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ReplInterpreterScriptLoaderTests
    {
        [Test]
        public void ConstructorPrefersNovaSharpPathEnvironmentVariable()
        {
            using EnvironmentScope scope = new EnvironmentScope(
                new Dictionary<string, string>
                {
                    { "NovaSharp_PATH", "Scripts/?;Scripts/?/init.lua" },
                    { "LUA_PATH_5_2", null },
                    { "LUA_PATH", null },
                }
            );

            TestReplLoader loader = new TestReplLoader();

            Assert.That(
                loader.ModulePaths,
                Is.EqualTo(new[] { "Scripts/?", "Scripts/?/init.lua" })
            );
        }

        [Test]
        public void ConstructorFallsBackToDefaultPaths()
        {
            using EnvironmentScope scope = new EnvironmentScope(
                new Dictionary<string, string>
                {
                    { "NovaSharp_PATH", null },
                    { "LUA_PATH_5_2", null },
                    { "LUA_PATH", null },
                }
            );

            TestReplLoader loader = new TestReplLoader();

            Assert.That(loader.ModulePaths, Is.EqualTo(new[] { "?", "?.lua" }));
        }

        [Test]
        public void ResolveModuleNameHonoursLuaPathGlobal()
        {
            using EnvironmentScope scope = new EnvironmentScope(
                new Dictionary<string, string>
                {
                    { "NovaSharp_PATH", null },
                    { "LUA_PATH_5_2", null },
                    { "LUA_PATH", null },
                }
            );

            TestReplLoader loader = new TestReplLoader();

            Script script = new Script(CoreModules.None);
            script.Globals.Set("LUA_PATH", DynValue.NewString("./modules/?.lua;./fallback/?.lua"));

            loader.ResolveModuleName("feature.util", script.Globals);

            Assert.Multiple(() =>
            {
                Assert.That(loader.CapturedModule, Is.EqualTo("feature.util"));
                Assert.That(
                    loader.CapturedPaths,
                    Is.EqualTo(new[] { "./modules/?.lua", "./fallback/?.lua" })
                );
            });
        }

        private sealed class TestReplLoader : ReplInterpreterScriptLoader
        {
            public IReadOnlyList<string> CapturedPaths { get; private set; }

            public string CapturedModule { get; private set; }

            protected override string ResolveModuleName(string modname, IEnumerable<string> paths)
            {
                CapturedModule = modname;
                CapturedPaths = paths?.ToArray();
                return "resolved.lua";
            }

            public new IReadOnlyList<string> ModulePaths => base.ModulePaths;
        }

        private sealed class EnvironmentScope : IDisposable
        {
            private readonly Dictionary<string, string> _originalValues = new();

            public EnvironmentScope(Dictionary<string, string> variables)
            {
                foreach ((string key, string value) in variables)
                {
                    _originalValues[key] = Environment.GetEnvironmentVariable(key);
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            public void Dispose()
            {
                foreach ((string key, string value) in _originalValues)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
    }
}
