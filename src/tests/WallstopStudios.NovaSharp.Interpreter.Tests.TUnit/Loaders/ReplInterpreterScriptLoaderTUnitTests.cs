namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Loaders
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.REPL;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ReplInterpreterScriptLoaderTUnitTests
    {
        private const string NovaSharpPathVariable = "NOVASHARP_PATH";
        private static readonly string[] NovaPreferredModulePaths = { "?/fromNova", "alt/?.lua" };
        private static readonly string[] Lua52ModulePaths = { "lua52/?.lua" };
        private static readonly string[] LuaGlobalModulePaths = { "luaGlobal/?.lua" };
        private static readonly string[] DefaultModulePaths = { "?", "?.lua" };

        [Test]
        public async Task ConstructorPrefersNovaSharpPathEnvironmentVariable()
        {
            TestReplLoader loader = CreateLoader(
                (NovaSharpPathVariable, "?/fromNova;alt/?.lua"),
                ("LUA_PATH_5_2", "ignored/?.lua"),
                ("LUA_PATH", "ignored2/?.lua")
            );

            await Assert
                .That(JoinPaths(loader.CapturedModulePaths))
                .IsEqualTo(JoinPaths(NovaPreferredModulePaths));
        }

        [Test]
        public async Task ConstructorFallsBackToLuaPath52ThenLuaPath()
        {
            TestReplLoader loader = CreateLoader(
                (NovaSharpPathVariable, null),
                ("LUA_PATH_5_2", "lua52/?.lua"),
                ("LUA_PATH", "luaGlobal/?.lua")
            );
            await Assert
                .That(JoinPaths(loader.CapturedModulePaths))
                .IsEqualTo(JoinPaths(Lua52ModulePaths));

            loader = CreateLoader((NovaSharpPathVariable, null), ("LUA_PATH", "luaGlobal/?.lua"));
            await Assert
                .That(JoinPaths(loader.CapturedModulePaths))
                .IsEqualTo(JoinPaths(LuaGlobalModulePaths));
        }

        [Test]
        public async Task ConstructorIgnoresEmptyNovaSharpPathValue()
        {
            TestReplLoader loader = CreateLoader(
                (NovaSharpPathVariable, "   "),
                ("LUA_PATH_5_2", "lua52/?.lua")
            );

            await Assert.That(loader.CapturedModulePaths).IsNotNull();
            await Assert
                .That(JoinPaths(loader.CapturedModulePaths))
                .IsEqualTo(JoinPaths(Lua52ModulePaths));
        }

        [Test]
        public async Task ConstructorFallsBackToDefaultPathWhenEnvironmentUnset()
        {
            TestReplLoader loader = CreateLoader();

            await Assert
                .That(JoinPaths(loader.CapturedModulePaths))
                .IsEqualTo(JoinPaths(DefaultModulePaths));
        }

        [Test]
        [AllLuaVersions]
        public async Task ResolveModuleNameUsesLuaPathGlobalWhenPresent(
            LuaCompatibilityVersion version
        )
        {
            TestReplLoader loader = CreateLoader();
            loader.SetModulePaths("global/?.lua");
            loader.MarkExisting("lua_path/pkg/mod.lua");

            Table globals = new(new Script(version));
            globals.Set("LUA_PATH", DynValue.NewString("lua_path/?.lua"));

            string result = loader.ResolveModuleName("pkg.mod", globals);
            await Assert.That(result).IsEqualTo("lua_path/pkg/mod.lua");
        }

        [Test]
        [AllLuaVersions]
        public async Task ResolveModuleNameFallsBackToModulePathsWhenLuaPathMissing(
            LuaCompatibilityVersion version
        )
        {
            TestReplLoader loader = CreateLoader();
            loader.SetModulePaths("global/?.lua");
            loader.MarkExisting("global/pkg/mod.lua");

            Table globals = new(new Script(version));

            string result = loader.ResolveModuleName("pkg.mod", globals);
            await Assert.That(result).IsEqualTo("global/pkg/mod.lua");
        }

        [Test]
        [AllLuaVersions]
        public async Task ResolveModuleNameIgnoresNonStringLuaPathGlobal(
            LuaCompatibilityVersion version
        )
        {
            TestReplLoader loader = CreateLoader();
            loader.SetModulePaths("global/?.lua");
            loader.MarkExisting("global/pkg/mod.lua");

            Table globals = new(new Script(version));
            globals.Set("LUA_PATH", DynValue.NewNumber(42));

            string result = loader.ResolveModuleName("pkg.mod", globals);
            await Assert.That(result).IsEqualTo("global/pkg/mod.lua");
        }

        [Test]
        [AllLuaVersions]
        public async Task ResolveModuleNameReturnsNullWhenLuaPathCannotResolve(
            LuaCompatibilityVersion version
        )
        {
            TestReplLoader loader = CreateLoader();
            loader.SetModulePaths("global/?.lua");

            Table globals = new(new Script(version));
            globals.Set("LUA_PATH", DynValue.NewString("missing/?.lua"));

            string result = loader.ResolveModuleName("pkg.mod", globals);
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task ResolveModuleNameThrowsWhenGlobalContextIsNull()
        {
            TestReplLoader loader = CreateLoader();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                loader.ResolveModuleName("pkg.mod", (Table)null);
            });

            await Assert.That(exception).IsNotNull();
            await Assert.That(exception.ParamName).IsEqualTo("globalContext");
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

        private static string JoinPaths(IReadOnlyList<string> paths) =>
            string.Join("|", paths ?? Array.Empty<string>());

        private sealed class TestReplLoader : ReplInterpreterScriptLoader
        {
            private readonly HashSet<string> _existingPaths = new(StringComparer.Ordinal);

            public TestReplLoader(Dictionary<string, string> environment)
                : base(envVar => environment.TryGetValue(envVar, out string value) ? value : null)
            { }

            public IReadOnlyList<string> CapturedModulePaths => ModulePaths;

            public void MarkExisting(params string[] paths)
            {
                foreach (string path in paths)
                {
                    _existingPaths.Add(path);
                }
            }

            public void SetModulePaths(params string[] paths) => ModulePaths = paths;

            public override bool ScriptFileExists(string name)
            {
                return _existingPaths.Contains(name);
            }
        }
    }
}
