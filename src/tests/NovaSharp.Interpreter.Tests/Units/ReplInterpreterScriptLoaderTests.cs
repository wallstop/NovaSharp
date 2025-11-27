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
    [Parallelizable(ParallelScope.Self)]
    public sealed class ReplInterpreterScriptLoaderTests
    {
        private const string NovaSharpPathVariable = "NOVASHARP_PATH";
        private static readonly string[] NovaPreferredModulePaths = { "?/fromNova", "alt/?.lua" };
        private static readonly string[] Lua52ModulePaths = { "lua52/?.lua" };
        private static readonly string[] LuaGlobalModulePaths = { "luaGlobal/?.lua" };
        private static readonly string[] DefaultModulePaths = { "?", "?.lua" };

        [Test]
        public void ConstructorPrefersNovaSharpPathEnvironmentVariable()
        {
            TestReplLoader loader = CreateLoader(
                (NovaSharpPathVariable, "?/fromNova;alt/?.lua"),
                ("LUA_PATH_5_2", "ignored/?.lua"),
                ("LUA_PATH", "ignored2/?.lua")
            );

            CollectionAssert.AreEqual(NovaPreferredModulePaths, loader.ModulePaths);
        }

        [Test]
        public void ConstructorFallsBackToLuaPath52ThenLuaPath()
        {
            TestReplLoader loader = CreateLoader(
                (NovaSharpPathVariable, null),
                ("LUA_PATH_5_2", "lua52/?.lua"),
                ("LUA_PATH", "luaGlobal/?.lua")
            );
            CollectionAssert.AreEqual(Lua52ModulePaths, loader.ModulePaths);

            loader = CreateLoader((NovaSharpPathVariable, null), ("LUA_PATH", "luaGlobal/?.lua"));
            CollectionAssert.AreEqual(LuaGlobalModulePaths, loader.ModulePaths);
        }

        [Test]
        public void ConstructorIgnoresEmptyNovaSharpPathValue()
        {
            TestReplLoader loader = CreateLoader(
                (NovaSharpPathVariable, "   "),
                ("LUA_PATH_5_2", "lua52/?.lua")
            );

            Assert.That(loader.ModulePaths, Is.Not.Null);
            CollectionAssert.AreEqual(Lua52ModulePaths, loader.ModulePaths);
        }

        [Test]
        public void ConstructorFallsBackToDefaultPathWhenEnvironmentUnset()
        {
            TestReplLoader loader = CreateLoader();

            CollectionAssert.AreEqual(DefaultModulePaths, loader.ModulePaths);
        }

        [Test]
        public void ResolveModuleNameUsesLuaPathGlobalWhenPresent()
        {
            TestReplLoader loader = CreateLoader();
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
            TestReplLoader loader = CreateLoader();
            loader.SetModulePaths("global/?.lua");
            loader.MarkExisting("global/pkg/mod.lua");

            Table globals = new(new Script());

            string result = loader.ResolveModuleName("pkg.mod", globals);
            Assert.That(result, Is.EqualTo("global/pkg/mod.lua"));
        }

        [Test]
        public void ResolveModuleNameIgnoresNonStringLuaPathGlobal()
        {
            TestReplLoader loader = CreateLoader();
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
            TestReplLoader loader = CreateLoader();
            loader.SetModulePaths("global/?.lua");

            Table globals = new(new Script());
            globals.Set("LUA_PATH", DynValue.NewString("missing/?.lua"));

            string result = loader.ResolveModuleName("pkg.mod", globals);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ResolveModuleNameThrowsWhenGlobalContextIsNull()
        {
            TestReplLoader loader = CreateLoader();

            Assert.That(
                () => loader.ResolveModuleName("pkg.mod", (Table)null),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("globalContext")
            );
        }

        private static TestReplLoader CreateLoader(
            params (string Name, string Value)[] environmentVariables
        )
        {
            Dictionary<string, string> env = new(StringComparer.OrdinalIgnoreCase);

            foreach ((string Name, string Value) pair in environmentVariables)
            {
                if (pair.Value != null)
                {
                    env[pair.Name] = pair.Value;
                }
            }

            return new TestReplLoader(env);
        }

        private sealed class TestReplLoader : ReplInterpreterScriptLoader
        {
            private readonly HashSet<string> _existingPaths = new(StringComparer.Ordinal);

            public TestReplLoader(Dictionary<string, string> environment)
                : base(envVar => environment.TryGetValue(envVar, out string value) ? value : null)
            { }

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
    }
}
