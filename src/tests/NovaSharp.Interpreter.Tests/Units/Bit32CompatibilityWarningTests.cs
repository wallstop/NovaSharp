namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class Bit32CompatibilityWarningTests
    {
        [Test]
        public void RequireBit32InLua53EmitsWarningOnlyOnce()
        {
            List<string> warnings = new();
            Script script = new(
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

            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("require('bit32')"));
            Assert.That(warnings[0], Does.Contain("Lua 5.3"));
        }

        private sealed class NullModuleScriptLoader : IScriptLoader
        {
            public object LoadFile(string file, Table globalContext)
            {
                throw new AssertionException("LoadFile should not be invoked for missing modules.");
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
    }
}
