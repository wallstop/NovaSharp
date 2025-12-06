namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Modding
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modding;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [ScriptGlobalOptionsIsolation]
    public sealed class ModManifestTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ParseAcceptsNumericCompatibilityTokens()
        {
            ModManifest manifest = ModManifest.Parse("{ \"luaCompatibility\": \"5.4\" }");

            await Assert
                .That(manifest.LuaCompatibility)
                .IsEqualTo(LuaCompatibilityVersion.Lua54)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryParseReturnsFalseForInvalidCompatibility()
        {
            bool parsed = ModManifest.TryParse(
                "{ \"luaCompatibility\": \"boom\" }",
                out ModManifest manifest,
                out string error
            );

            await Assert.That(parsed).IsFalse().ConfigureAwait(false);
            await Assert.That(manifest).IsNull().ConfigureAwait(false);
            await Assert.That(error).Contains("Unable to parse").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryParseReturnsTrueForValidManifest()
        {
            bool parsed = ModManifest.TryParse(
                "{ \"name\": \"sample\", \"luaCompatibility\": \"Lua54\" }",
                out ModManifest manifest,
                out string error
            );

            await Assert.That(parsed).IsTrue().ConfigureAwait(false);
            await Assert.That(manifest.Name).IsEqualTo("sample").ConfigureAwait(false);
            await Assert.That(error).IsNull().ConfigureAwait(false);
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
                .IsEqualTo(Script.DefaultOptions.CompatibilityVersion)
                .ConfigureAwait(false);
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
                .IsEqualTo(LuaCompatibilityVersion.Latest)
                .ConfigureAwait(false);
            await Assert.That(warning).Contains("Example").ConfigureAwait(false);
            await Assert.That(warning).Contains("Lua 5.5").ConfigureAwait(false);
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
                .IsEqualTo(LuaCompatibilityVersion.Lua53)
                .ConfigureAwait(false);
            await Assert.That(warned).IsFalse().ConfigureAwait(false);
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

            await Assert.That(manifest.Name).IsEqualTo("streamed").ConfigureAwait(false);
            await Assert.That(manifest.Version).IsEqualTo("2.0").ConfigureAwait(false);
            await Assert
                .That(manifest.LuaCompatibility)
                .IsEqualTo(LuaCompatibilityVersion.Lua52)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryParseReturnsFalseWhenJsonWhitespace()
        {
            bool parsed = ModManifest.TryParse("  ", out ModManifest manifest, out string error);

            await Assert.That(parsed).IsFalse().ConfigureAwait(false);
            await Assert.That(manifest).IsNull().ConfigureAwait(false);
            await Assert.That(error).Contains("cannot be empty").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TryParseReturnsFalseWhenJsonMalformed()
        {
            bool parsed = ModManifest.TryParse(
                "{ invalid",
                out ModManifest manifest,
                out string error
            );

            await Assert.That(parsed).IsFalse().ConfigureAwait(false);
            await Assert.That(manifest).IsNull().ConfigureAwait(false);
            await Assert.That(error).IsNotNull().ConfigureAwait(false);
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

            await Assert.That(parsed).IsFalse().ConfigureAwait(false);
            await Assert.That(manifest).IsNull().ConfigureAwait(false);
            await Assert
                .That(error)
                .IsEqualTo("Manifest JSON resolved to an empty document.")
                .ConfigureAwait(false);
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
                .IsEqualTo(LuaCompatibilityVersion.Latest)
                .ConfigureAwait(false);
            await Assert
                .That(warning.StartsWith("mod targets", StringComparison.OrdinalIgnoreCase))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ApplyCompatibilityFallsBackToGlobalOptionsWhenHostOmitted()
        {
            ModManifest manifest = new("Compat", "1.0", LuaCompatibilityVersion.Lua54);
            ScriptOptions options = new(Script.DefaultOptions);
            using ScriptGlobalOptionsScope globalScope = ScriptGlobalOptionsScope.Override(opts =>
                opts.CompatibilityVersion = LuaCompatibilityVersion.Lua53
            );

            string warning = null;
            manifest.ApplyCompatibility(
                options,
                hostCompatibility: null,
                warningSink: message => warning = message
            );

            await Assert
                .That(options.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua54)
                .ConfigureAwait(false);
            await Assert.That(warning).Contains("Lua 5.3").ConfigureAwait(false);
        }
    }
}
