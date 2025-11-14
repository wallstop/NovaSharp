namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class UnityAssetsScriptLoaderTests
    {
        [Test]
        public void LoadFileReturnsResourceContentRegardlessOfPath()
        {
            Dictionary<string, string> resources = new(StringComparer.OrdinalIgnoreCase)
            {
                ["init.lua"] = "print('hi')",
            };

            UnityAssetsScriptLoader loader = new(resources);

            object script = loader.LoadFile("scripts/init.lua", null!);

            Assert.That(script, Is.EqualTo("print('hi')"));
        }

        [Test]
        public void LoadFileThrowsHelpfulMessageWhenMissing()
        {
            UnityAssetsScriptLoader loader = new(new Dictionary<string, string>());

            Exception ex = Assert.Throws<Exception>(() => loader.LoadFile("missing.lua", null!));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain(UnityAssetsScriptLoader.DefaultPath));
        }

        [Test]
        public void ScriptFileExistsHandlesPathsAndExtensions()
        {
            UnityAssetsScriptLoader loader = new(
                new Dictionary<string, string>
                {
                    ["secondary.lua"] = "",
                }
            );

            Assert.Multiple(() =>
            {
                Assert.That(loader.ScriptFileExists("secondary.lua"), Is.True);
                Assert.That(loader.ScriptFileExists("Scripts/secondary.lua"), Is.True);
                Assert.That(loader.ScriptFileExists("Scripts/other.lua"), Is.False);
            });
        }

        [Test]
        public void GetLoadedScriptsReturnsSnapshotOfKeys()
        {
            Dictionary<string, string> resources = new()
            {
                ["alpha.lua"] = "",
                ["beta.lua"] = "",
            };

            UnityAssetsScriptLoader loader = new(resources);

            string[] loaded = loader.GetLoadedScripts();

            Assert.That(loaded, Is.EquivalentTo(new[] { "alpha.lua", "beta.lua" }));
        }
    }
}
