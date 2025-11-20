namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modding;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ModManifestCompatibilityTests
    {
        [Test]
        public void TryApplyFromScriptPathAppliesManifestAndLogsInfo()
        {
            string tempDir = CreateTempDirectory();
            string scriptPath = Path.Combine(tempDir, "main.lua");
            File.WriteAllText(scriptPath, "return 1");

            File.WriteAllText(
                Path.Combine(tempDir, "mod.json"),
                "{ \"luaCompatibility\": \"Lua53\" }"
            );

            ScriptOptions options = new(Script.DefaultOptions);
            List<string> info = new();
            List<string> warnings = new();

            bool applied = ModManifestCompatibility.TryApplyFromScriptPath(
                scriptPath,
                options,
                LuaCompatibilityVersion.Lua55,
                info.Add,
                warnings.Add
            );

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.True);
                Assert.That(
                    options.CompatibilityVersion,
                    Is.EqualTo(LuaCompatibilityVersion.Lua53)
                );
                Assert.That(info, Has.Count.EqualTo(1));
                Assert.That(info[0], Does.Contain("Lua 5.3"));
                Assert.That(warnings, Is.Empty);
            });
        }

        [Test]
        public void TryApplyFromDirectoryHandlesInvalidManifest()
        {
            string tempDir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(tempDir, "mod.json"), "{ this is: not json");

            ScriptOptions options = new(Script.DefaultOptions);
            List<string> warnings = new();

            bool applied = ModManifestCompatibility.TryApplyFromDirectory(
                tempDir,
                options,
                LuaCompatibilityVersion.Lua55,
                infoSink: null,
                warningSink: warnings.Add
            );

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.False);
                Assert.That(warnings, Has.Count.EqualTo(1));
                Assert.That(warnings[0], Does.Contain("Failed to load"));
            });
        }

        [Test]
        public void CreateScriptFromDirectoryAppliesCompatibilityBeforeInstantiation()
        {
            string tempDir = CreateTempDirectory();
            File.WriteAllText(
                Path.Combine(tempDir, "mod.json"),
                "{ \"name\": \"mod\", \"luaCompatibility\": \"Lua52\" }"
            );

            List<string> info = new();

            Script script = ModManifestCompatibility.CreateScriptFromDirectory(
                tempDir,
                CoreModules.Basic,
                baseOptions: null,
                infoSink: info.Add,
                warningSink: null
            );

            Assert.Multiple(() =>
            {
                Assert.That(script.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Lua52));
                Assert.That(info, Is.Not.Empty);
                Assert.That(info[0], Does.Contain("Lua 5.2"));
            });
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                $"novasharp_modcompat_{Guid.NewGuid():N}"
            );
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
