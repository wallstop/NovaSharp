#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modding;
    using NovaSharp.Interpreter.Modules;

    [ScriptGlobalOptionsIsolation]
    public sealed class ModManifestTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ParseAcceptsNumericCompatibilityTokens()
        {
            ModManifest manifest = ModManifest.Parse("{ \"luaCompatibility\": \"5.4\" }");

            await Assert.That(manifest.LuaCompatibility).IsEqualTo(LuaCompatibilityVersion.Lua54);
        }

        [global::TUnit.Core.Test]
        public async Task TryParseReturnsFalseForInvalidCompatibility()
        {
            bool parsed = ModManifest.TryParse(
                "{ \"luaCompatibility\": \"boom\" }",
                out ModManifest manifest,
                out string error
            );

            await Assert.That(parsed).IsFalse();
            await Assert.That(manifest).IsNull();
            await Assert.That(error).Contains("Unable to parse");
        }

        [global::TUnit.Core.Test]
        public async Task TryParseReturnsTrueForValidManifest()
        {
            bool parsed = ModManifest.TryParse(
                "{ \"name\": \"sample\", \"luaCompatibility\": \"Lua54\" }",
                out ModManifest manifest,
                out string error
            );

            await Assert.That(parsed).IsTrue();
            await Assert.That(manifest.Name).IsEqualTo("sample");
            await Assert.That(error).IsNull();
        }

        [global::TUnit.Core.Test]
        public void ParseThrowsWhenJsonEmpty()
        {
            Assert.Throws<ArgumentException>(() => ModManifest.Parse(" "));
        }

        [global::TUnit.Core.Test]
        public void LoadThrowsWhenStreamNull()
        {
            Assert.Throws<ArgumentNullException>(() => ModManifest.Load(null));
        }

        [global::TUnit.Core.Test]
        public async Task ApplyCompatibilityDoesNothingWhenManifestHasNoVersion()
        {
            ModManifest manifest = new(null, null, null);
            ScriptOptions options = new(Script.DefaultOptions);

            manifest.ApplyCompatibility(options, LuaCompatibilityVersion.Lua54, warningSink: null);

            await Assert
                .That(options.CompatibilityVersion)
                .IsEqualTo(Script.DefaultOptions.CompatibilityVersion);
        }

        [global::TUnit.Core.Test]
        public async Task ApplyCompatibilityWarnsWhenRequestedVersionNewerThanHost()
        {
            ModManifest manifest = new("Example", "1.0", LuaCompatibilityVersion.Latest);
            ScriptOptions options = new(Script.DefaultOptions);
            string warning = null;

            manifest.ApplyCompatibility(
                options,
                hostCompatibility: LuaCompatibilityVersion.Lua53,
                warningSink: message => warning = message
            );

            await Assert
                .That(options.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Latest);
            await Assert.That(warning).Contains("Example");
            await Assert.That(warning).Contains("Lua 5.5");
        }

        [global::TUnit.Core.Test]
        public async Task ApplyCompatibilityDoesNotWarnWhenHostSupportsRequestedVersion()
        {
            ModManifest manifest = new("Example", "1.0", LuaCompatibilityVersion.Lua53);
            ScriptOptions options = new(Script.DefaultOptions);
            bool warned = false;

            manifest.ApplyCompatibility(
                options,
                hostCompatibility: LuaCompatibilityVersion.Lua55,
                warningSink: _ => warned = true
            );

            await Assert
                .That(options.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua53);
            await Assert.That(warned).IsFalse();
        }

        [global::TUnit.Core.Test]
        public void ApplyCompatibilityThrowsWhenOptionsNull()
        {
            ModManifest manifest = new("Example", "1.0", LuaCompatibilityVersion.Lua53);

            Assert.Throws<ArgumentNullException>(() => manifest.ApplyCompatibility(null));
        }

        [global::TUnit.Core.Test]
        public void ParseThrowsWhenCompatibilityDigitsMissing()
        {
            Assert.Throws<InvalidDataException>(() =>
                ModManifest.Parse("{ \"luaCompatibility\": \"???\" }")
            );
        }

        [global::TUnit.Core.Test]
        public async Task LoadParsesManifestFromStream()
        {
            using MemoryStream stream = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    "{ \"name\": \"streamed\", \"version\": \"2.0\", \"luaCompatibility\": \"Lua52\" }"
                )
            );

            ModManifest manifest = ModManifest.Load(stream);

            await Assert.That(manifest.Name).IsEqualTo("streamed");
            await Assert.That(manifest.Version).IsEqualTo("2.0");
            await Assert.That(manifest.LuaCompatibility).IsEqualTo(LuaCompatibilityVersion.Lua52);
        }

        [global::TUnit.Core.Test]
        public async Task TryParseReturnsFalseWhenJsonWhitespace()
        {
            bool parsed = ModManifest.TryParse("  ", out ModManifest manifest, out string error);

            await Assert.That(parsed).IsFalse();
            await Assert.That(manifest).IsNull();
            await Assert.That(error).Contains("cannot be empty");
        }

        [global::TUnit.Core.Test]
        public async Task TryParseReturnsFalseWhenJsonMalformed()
        {
            bool parsed = ModManifest.TryParse(
                "{ invalid",
                out ModManifest manifest,
                out string error
            );

            await Assert.That(parsed).IsFalse();
            await Assert.That(manifest).IsNull();
            await Assert.That(error).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public void ParseThrowsWhenJsonResolvesToNullDocument()
        {
            Assert.Throws<InvalidDataException>(() => ModManifest.Parse("null"));
        }

        [global::TUnit.Core.Test]
        public async Task TryParseReturnsFalseWhenJsonResolvesToNullDocument()
        {
            bool parsed = ModManifest.TryParse("null", out ModManifest manifest, out string error);

            await Assert.That(parsed).IsFalse();
            await Assert.That(manifest).IsNull();
            await Assert.That(error).IsEqualTo("Manifest JSON resolved to an empty document.");
        }

        [global::TUnit.Core.Test]
        public async Task ApplyCompatibilityUsesGenericLabelWhenNameMissing()
        {
            ModManifest manifest = new(null, "1.0", LuaCompatibilityVersion.Latest);
            ScriptOptions options = new(Script.DefaultOptions);
            string warning = null;

            manifest.ApplyCompatibility(
                options,
                hostCompatibility: LuaCompatibilityVersion.Lua54,
                warningSink: message => warning = message
            );

            await Assert
                .That(options.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Latest);
            await Assert
                .That(warning.StartsWith("mod targets", StringComparison.OrdinalIgnoreCase))
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ApplyCompatibilityFallsBackToGlobalOptionsWhenHostOmitted()
        {
            ModManifest manifest = new("Compat", "1.0", LuaCompatibilityVersion.Lua54);
            ScriptOptions options = new(Script.DefaultOptions);
            LuaCompatibilityVersion original = Script.GlobalOptions.CompatibilityVersion;
            Script.GlobalOptions.CompatibilityVersion = LuaCompatibilityVersion.Lua53;

            try
            {
                string warning = null;
                manifest.ApplyCompatibility(
                    options,
                    hostCompatibility: null,
                    warningSink: message => warning = message
                );

                await Assert
                    .That(options.CompatibilityVersion)
                    .IsEqualTo(LuaCompatibilityVersion.Lua54);
                await Assert.That(warning).Contains("Lua 5.3");
            }
            finally
            {
                Script.GlobalOptions.CompatibilityVersion = original;
            }
        }
    }
}
#pragma warning restore CA2007
