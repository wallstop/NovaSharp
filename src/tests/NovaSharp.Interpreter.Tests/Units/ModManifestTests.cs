namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modding;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ModManifestTests
    {
        [Test]
        public void ParseAcceptsNumericCompatibilityTokens()
        {
            ModManifest manifest = ModManifest.Parse("{ \"luaCompatibility\": \"5.4\" }");

            Assert.That(manifest.LuaCompatibility, Is.EqualTo(LuaCompatibilityVersion.Lua54));
        }

        [Test]
        public void TryParseReturnsFalseForInvalidCompatibility()
        {
            bool parsed = ModManifest.TryParse(
                "{ \"luaCompatibility\": \"boom\" }",
                out ModManifest manifest,
                out string error
            );

            Assert.Multiple(() =>
            {
                Assert.That(parsed, Is.False);
                Assert.That(manifest, Is.Null);
                Assert.That(error, Does.Contain("Unable to parse"));
            });
        }

        [Test]
        public void TryParseReturnsTrueForValidManifest()
        {
            bool parsed = ModManifest.TryParse(
                "{ \"name\": \"sample\", \"luaCompatibility\": \"Lua54\" }",
                out ModManifest manifest,
                out string error
            );

            Assert.Multiple(() =>
            {
                Assert.That(parsed, Is.True);
                Assert.That(manifest.Name, Is.EqualTo("sample"));
                Assert.That(error, Is.Null);
            });
        }

        [Test]
        public void ParseThrowsWhenJsonEmpty()
        {
            Assert.That(
                () => ModManifest.Parse(" "),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("json")
            );
        }

        [Test]
        public void LoadThrowsWhenStreamNull()
        {
            Assert.That(
                () => ModManifest.Load(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("stream")
            );
        }

        [Test]
        public void ApplyCompatibilityDoesNothingWhenManifestHasNoVersion()
        {
            ModManifest manifest = new(null, null, null);
            ScriptOptions options = new(Script.DefaultOptions);

            manifest.ApplyCompatibility(options, LuaCompatibilityVersion.Lua54, warningSink: null);

            Assert.That(
                options.CompatibilityVersion,
                Is.EqualTo(Script.DefaultOptions.CompatibilityVersion)
            );
        }

        [Test]
        public void ApplyCompatibilityWarnsWhenRequestedVersionNewerThanHost()
        {
            ModManifest manifest = new("Example", "1.0", LuaCompatibilityVersion.Latest);
            ScriptOptions options = new(Script.DefaultOptions);
            string warning = null;

            manifest.ApplyCompatibility(
                options,
                hostCompatibility: LuaCompatibilityVersion.Lua53,
                warningSink: message => warning = message
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    options.CompatibilityVersion,
                    Is.EqualTo(LuaCompatibilityVersion.Latest)
                );
                Assert.That(warning, Does.Contain("Example"));
                Assert.That(warning, Does.Contain("Lua 5.5"));
            });
        }

        [Test]
        public void ApplyCompatibilityDoesNotWarnWhenHostSupportsRequestedVersion()
        {
            ModManifest manifest = new("Example", "1.0", LuaCompatibilityVersion.Lua53);
            ScriptOptions options = new(Script.DefaultOptions);
            bool warned = false;

            manifest.ApplyCompatibility(
                options,
                hostCompatibility: LuaCompatibilityVersion.Lua55,
                warningSink: _ => warned = true
            );

            Assert.Multiple(() =>
            {
                Assert.That(
                    options.CompatibilityVersion,
                    Is.EqualTo(LuaCompatibilityVersion.Lua53)
                );
                Assert.That(warned, Is.False);
            });
        }

        [Test]
        public void ApplyCompatibilityThrowsWhenOptionsNull()
        {
            ModManifest manifest = new("Example", "1.0", LuaCompatibilityVersion.Lua53);

            Assert.That(
                () => manifest.ApplyCompatibility(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options")
            );
        }

        [Test]
        public void ParseThrowsWhenCompatibilityDigitsMissing()
        {
            Assert.That(
                () => ModManifest.Parse("{ \"luaCompatibility\": \"???\" }"),
                Throws.TypeOf<InvalidDataException>()
            );
        }
    }
}
