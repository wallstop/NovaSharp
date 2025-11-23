#nullable enable
namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.REPL;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;

    [TestFixture]
    [NonParallelizable]
    public sealed class ReplInterpreterScriptLoaderTests
    {
        private static IDisposable OverrideEnv(string name, string? value)
        {
            string? original = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
            return new EnvRestore(name, original);
        }

        [Test]
        public void ConstructorPrefersNovaSharpPathEnvironmentVariable()
        {
            using IDisposable novaPath = OverrideEnv("NovaSharp_PATH", "?/fromNova;alt/?.lua");
            using IDisposable lua52 = OverrideEnv("LUA_PATH_5_2", "ignored/?.lua");
            using IDisposable lua = OverrideEnv("LUA_PATH", "ignored2/?.lua");

            TestReplLoader loader = new();

            CollectionAssert.AreEqual(new[] { "?/fromNova", "alt/?.lua" }, loader.ModulePaths);
        }

        [Test]
        public void ConstructorFallsBackToLuaPath52ThenLuaPath()
        {
            using IDisposable novaPath = OverrideEnv("NovaSharp_PATH", null);
            using IDisposable lua52 = OverrideEnv("LUA_PATH_5_2", "lua52/?.lua");
            using IDisposable lua = OverrideEnv("LUA_PATH", "luaGlobal/?.lua");

            TestReplLoader loader = new();
            CollectionAssert.AreEqual(new[] { "lua52/?.lua" }, loader.ModulePaths);

            using IDisposable clearLua52 = OverrideEnv("LUA_PATH_5_2", null);
            loader = new TestReplLoader();
            CollectionAssert.AreEqual(new[] { "luaGlobal/?.lua" }, loader.ModulePaths);
        }

        [Test]
        public void ConstructorIgnoresEmptyNovaSharpPathValue()
        {
            using IDisposable novaPath = OverrideEnv("NovaSharp_PATH", "   ");
            using IDisposable lua52 = OverrideEnv("LUA_PATH_5_2", "lua52/?.lua");
            using IDisposable lua = OverrideEnv("LUA_PATH", null);

            TestReplLoader loader = new();

            Assert.That(loader.ModulePaths, Is.Not.Null);
            CollectionAssert.AreEqual(new[] { "lua52/?.lua" }, loader.ModulePaths);
        }

        [Test]
        public void ConstructorFallsBackToDefaultPathWhenEnvironmentUnset()
        {
            using IDisposable novaPath = OverrideEnv("NovaSharp_PATH", null);
            using IDisposable lua52 = OverrideEnv("LUA_PATH_5_2", null);
            using IDisposable lua = OverrideEnv("LUA_PATH", null);

            TestReplLoader loader = new();

            CollectionAssert.AreEqual(new[] { "?", "?.lua" }, loader.ModulePaths);
        }

        [Test]
        public void ResolveModuleNameUsesLuaPathGlobalWhenPresent()
        {
            using IDisposable novaPath = OverrideEnv("NovaSharp_PATH", null);
            TestReplLoader loader = new();
            loader.SetModulePaths("global/?.lua");
            loader.MarkExisting("lua_path/pkg/mod.lua");

            Table globals = new(new Script());
            globals.Set("LUA_PATH", DynValue.NewString("lua_path/?.lua"));

            string result = loader.ResolveModuleName("pkg.mod", globals);
            Assert.That(result, Is.EqualTo("lua_path/pkg/mod.lua"));
        }

        [Test]
        public void ResolveModuleNameFallsBackToModulePathsWhenLuaPathMissing()
        {
            using IDisposable novaPath = OverrideEnv("NovaSharp_PATH", null);
            TestReplLoader loader = new();
            loader.SetModulePaths("global/?.lua");
            loader.MarkExisting("global/pkg/mod.lua");

            Table globals = new(new Script());

            string result = loader.ResolveModuleName("pkg.mod", globals);
            Assert.That(result, Is.EqualTo("global/pkg/mod.lua"));
        }

        [Test]
        public void ResolveModuleNameIgnoresNonStringLuaPathGlobal()
        {
            using IDisposable novaPath = OverrideEnv("NovaSharp_PATH", null);
            TestReplLoader loader = new();
            loader.SetModulePaths("global/?.lua");
            loader.MarkExisting("global/pkg/mod.lua");

            Table globals = new(new Script());
            globals.Set("LUA_PATH", DynValue.NewNumber(42));

            string result = loader.ResolveModuleName("pkg.mod", globals);
            Assert.That(result, Is.EqualTo("global/pkg/mod.lua"));
        }

        [Test]
        public void ResolveModuleNameReturnsNullWhenLuaPathCannotResolve()
        {
            using IDisposable novaPath = OverrideEnv("NovaSharp_PATH", null);
            TestReplLoader loader = new();
            loader.SetModulePaths("global/?.lua");

            Table globals = new(new Script());
            globals.Set("LUA_PATH", DynValue.NewString("missing/?.lua"));

            string result = loader.ResolveModuleName("pkg.mod", globals);
            Assert.That(result, Is.Null);
        }

        private sealed class TestReplLoader : ReplInterpreterScriptLoader
        {
            private readonly HashSet<string> _existingPaths = new(StringComparer.Ordinal);

            public void MarkExisting(params string[] paths)
            {
                foreach (string path in paths)
                {
                    _existingPaths.Add(path);
                }
            }

            public void SetModulePaths(params string[] paths)
            {
                ModulePaths = paths;
            }

            public override bool ScriptFileExists(string name)
            {
                return _existingPaths.Contains(name);
            }
        }

        private sealed class EnvRestore : IDisposable
        {
            private readonly string _name;
            private readonly string? _original;

            public EnvRestore(string name, string? original)
            {
                _name = name;
                _original = original;
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable(_name, _original);
            }
        }
    }
}
#nullable disable
