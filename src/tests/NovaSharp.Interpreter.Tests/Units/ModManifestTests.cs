namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modding;
    using NUnit.Framework;

    [TestFixture]
    public class ModManifestTests
    {
        [Test]
        public void ParsesLuaCompatibilityFromCanonicalName()
        {
            string json =
                "{\n"
                + "    \"name\": \"Example\",\n"
                + "    \"luaCompatibility\": \"Lua53\"\n"
                + "}";

            ModManifest manifest = ModManifest.Parse(json);

            Assert.That(manifest.Name, Is.EqualTo("Example"));
            Assert.That(manifest.LuaCompatibility, Is.EqualTo(LuaCompatibilityVersion.Lua53));
        }

        [Test]
        public void ParsesLuaCompatibilityFromNumericNotation()
        {
            string json =
                "{\n"
                + "    \"name\": \"Numeric\",\n"
                + "    \"luaCompatibility\": \"5.4\"\n"
                + "}";

            ModManifest manifest = ModManifest.Parse(json);

            Assert.That(manifest.LuaCompatibility, Is.EqualTo(LuaCompatibilityVersion.Lua54));
        }

        [Test]
        public void ApplyCompatibilitySetsScriptOptionsAndWarnsWhenHostIsOlder()
        {
            string json =
                "{\n"
                + "    \"name\": \"CompatibilityMod\",\n"
                + "    \"luaCompatibility\": \"Lua54\"\n"
                + "}";

            ModManifest manifest = ModManifest.Parse(json);
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions);
            List<string> warnings = new();

            manifest.ApplyCompatibility(options, LuaCompatibilityVersion.Lua53, warnings.Add);

            Assert.That(options.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Lua54));
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("Lua 5.4"));
        }

        [Test]
        public void ApplyCompatibilityDoesNotWarnWhenHostMatchesOrExceedsRequest()
        {
            string json =
                "{\n"
                + "    \"name\": \"Latest\",\n"
                + "    \"luaCompatibility\": \"latest\"\n"
                + "}";

            ModManifest manifest = ModManifest.Parse(json);
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions);
            List<string> warnings = new();

            manifest.ApplyCompatibility(options, LuaCompatibilityVersion.Lua55, warnings.Add);

            Assert.That(options.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Latest));
            Assert.That(warnings, Is.Empty);
        }
    }
}
