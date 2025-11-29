#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Modules;

    public sealed class Bit32CompatibilityWarningTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task RequireBit32InLua53EmitsWarningOnlyOnce()
        {
            List<string> warnings = new();
            Script script = new(
                CoreModules.PresetComplete,
                new ScriptOptions
                {
                    CompatibilityVersion = LuaCompatibilityVersion.Lua53,
                    DebugPrint = warnings.Add,
                    ScriptLoader = new NullModuleScriptLoader(),
                }
            );

            script.DoString(
                @"
                local ok1 = pcall(function() require('bit32') end)
                local ok2 = pcall(function() require('bit32') end)
                assert(ok1 == false and ok2 == false)
                "
            );

            await Assert.That(warnings.Count).IsEqualTo(1);
            await Assert.That(warnings[0]).Contains("require('bit32')");
            await Assert.That(warnings[0]).Contains("Lua 5.3");
        }

        [global::TUnit.Core.Test]
        public async Task RequireBit32InLua52DoesNotEmitWarning()
        {
            List<string> warnings = new();
            Script script = new(
                CoreModules.PresetComplete,
                new ScriptOptions
                {
                    CompatibilityVersion = LuaCompatibilityVersion.Lua52,
                    DebugPrint = warnings.Add,
                    ScriptLoader = new Bit32ModuleScriptLoader(),
                }
            );

            DynValue result = script.DoString("return require('bit32') ~= nil");

            await Assert.That(result.Boolean).IsTrue();
            await Assert.That(warnings.Count).IsZero();
        }

        private sealed class NullModuleScriptLoader : IScriptLoader
        {
            public object LoadFile(string file, Table globalContext)
            {
                throw new InvalidOperationException(
                    "LoadFile should not be invoked for missing modules."
                );
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return null;
            }
        }

        private sealed class Bit32ModuleScriptLoader : IScriptLoader
        {
            public object LoadFile(string file, Table globalContext)
            {
                if (string.Equals(file, "bit32.lua", StringComparison.OrdinalIgnoreCase))
                {
                    return "return { value = 123 }";
                }

                throw new InvalidOperationException($"Unexpected load request for '{file}'.");
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return string.Equals(modname, "bit32", StringComparison.OrdinalIgnoreCase)
                    ? "bit32.lua"
                    : null;
            }
        }
    }
}
#pragma warning restore CA2007
