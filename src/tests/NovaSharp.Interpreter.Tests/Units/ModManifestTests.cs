namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
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

        [Test]
        public void ParseThrowsWhenJsonIsEmpty()
        {
            Assert.That(
                () => ModManifest.Parse("  "),
                Throws
                    .TypeOf<ArgumentException>()
                    .With.Message.Contains("Manifest JSON cannot be empty.")
            );
        }

        [Test]
        public void ParseThrowsWhenDocumentResolvesToNull()
        {
            Assert.That(
                () => ModManifest.Parse("null"),
                Throws
                    .TypeOf<InvalidDataException>()
                    .With.Message.Contains("resolved to an empty document")
            );
        }

        [Test]
        public void LoadReadsManifestFromUtf8Stream()
        {
            string json =
                "{\n"
                + "    \"name\": \"StreamMod\",\n"
                + "    \"version\": \"1.0.0\",\n"
                + "    \"luaCompatibility\": \"Lua52\"\n"
                + "}";

            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            ModManifest manifest = ModManifest.Load(stream);

            Assert.Multiple(() =>
            {
                Assert.That(manifest.Name, Is.EqualTo("StreamMod"));
                Assert.That(manifest.Version, Is.EqualTo("1.0.0"));
                Assert.That(manifest.LuaCompatibility, Is.EqualTo(LuaCompatibilityVersion.Lua52));
            });
        }

        [Test]
        public void LoadThrowsWhenStreamIsNull()
        {
            Assert.That(() => ModManifest.Load(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TryParseReturnsFalseWhenJsonIsWhitespace()
        {
            bool success = ModManifest.TryParse("   ", out ModManifest manifest, out string error);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False);
                Assert.That(manifest, Is.Null);
                Assert.That(error, Does.Contain("Manifest JSON cannot be empty."));
            });
        }

        [Test]
        public void TryParseReturnsFalseWhenCompatibilityCannotBeParsed()
        {
            string json =
                "{\n"
                + "    \"name\": \"BadCompat\",\n"
                + "    \"luaCompatibility\": \"Lua999\"\n"
                + "}";

            bool success = ModManifest.TryParse(json, out ModManifest manifest, out string error);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False);
                Assert.That(manifest, Is.Null);
                Assert.That(error, Does.Contain("Unable to parse luaCompatibility value"));
            });
        }

        [Test]
        public void TryParseReturnsFalseWhenCompatibilityContainsNoDigits()
        {
            string json =
                "{\n"
                + "    \"name\": \"NoDigits\",\n"
                + "    \"luaCompatibility\": \"unsupported\"\n"
                + "}";

            bool success = ModManifest.TryParse(json, out ModManifest manifest, out string error);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False);
                Assert.That(manifest, Is.Null);
                Assert.That(error, Does.Contain("Unable to parse luaCompatibility value"));
            });
        }

        [Test]
        public void TryParseReturnsTrueForValidManifest()
        {
            string json =
                "{\n"
                + "    \"name\": \"Valid\",\n"
                + "    \"version\": \"2.0.0\",\n"
                + "    \"luaCompatibility\": \"Lua53\"\n"
                + "}";

            bool success = ModManifest.TryParse(json, out ModManifest manifest, out string error);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(manifest, Is.Not.Null);
                Assert.That(error, Is.Null);
                Assert.That(manifest.Name, Is.EqualTo("Valid"));
                Assert.That(manifest.Version, Is.EqualTo("2.0.0"));
                Assert.That(manifest.LuaCompatibility, Is.EqualTo(LuaCompatibilityVersion.Lua53));
            });
        }

        [Test]
        public void ApplyCompatibilityThrowsWhenOptionsAreNull()
        {
            ModManifest manifest = ModManifest.Parse(
                "{ \"name\": \"NullOptions\", \"luaCompatibility\": \"Lua54\" }"
            );

            Assert.That(
                () => manifest.ApplyCompatibility(null),
                Throws.TypeOf<ArgumentNullException>()
            );
        }

        [Test]
        public void ApplyCompatibilitySkipsWhenManifestHasNoCompatibilityRequest()
        {
            ModManifest manifest = ModManifest.Parse("{ \"name\": \"NoRequest\" }");
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions);
            LuaCompatibilityVersion initialVersion = options.CompatibilityVersion;
            List<string> warnings = new();

            manifest.ApplyCompatibility(options, LuaCompatibilityVersion.Lua53, warnings.Add);

            Assert.Multiple(() =>
            {
                Assert.That(options.CompatibilityVersion, Is.EqualTo(initialVersion));
                Assert.That(warnings, Is.Empty);
            });
        }

        [Test]
        public void ApplyCompatibilityUsesGlobalOptionsWhenHostNotProvided()
        {
            LuaCompatibilityVersion originalGlobal = Script.GlobalOptions.CompatibilityVersion;
            Script.GlobalOptions.CompatibilityVersion = LuaCompatibilityVersion.Lua53;

            try
            {
                ModManifest manifest = ModManifest.Parse(
                    "{ \"name\": \"GlobalAware\", \"luaCompatibility\": \"Lua55\" }"
                );
                ScriptOptions options = new ScriptOptions(Script.DefaultOptions);
                List<string> warnings = new();

                manifest.ApplyCompatibility(options, warningSink: warnings.Add);

                Assert.Multiple(() =>
                {
                    Assert.That(
                        options.CompatibilityVersion,
                        Is.EqualTo(LuaCompatibilityVersion.Lua55)
                    );
                    Assert.That(warnings, Has.Count.EqualTo(1));
                    Assert.That(warnings[0], Does.Contain("GlobalAware"));
                });
            }
            finally
            {
                Script.GlobalOptions.CompatibilityVersion = originalGlobal;
            }
        }

        [Test]
        public void ApplyCompatibilityWarnsUsingFallbackLabelWhenNameMissing()
        {
            string json =
                "{\n" + "    \"name\": \"\",\n" + "    \"luaCompatibility\": \"Lua55\"\n" + "}";

            ModManifest manifest = ModManifest.Parse(json);
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions);
            List<string> warnings = new();

            manifest.ApplyCompatibility(options, LuaCompatibilityVersion.Lua54, warnings.Add);

            Assert.Multiple(() =>
            {
                Assert.That(warnings, Has.Count.EqualTo(1));
                Assert.That(warnings[0], Does.StartWith("mod targets"));
            });
        }
    }
}
