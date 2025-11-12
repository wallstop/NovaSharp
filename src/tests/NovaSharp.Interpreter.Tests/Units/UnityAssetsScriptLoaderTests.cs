namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class UnityAssetsScriptLoaderTests
    {
        [Test]
        public void LoadFileReturnsScriptBodyWhenPresent()
        {
            Dictionary<string, string> scripts = new() { ["example.lua"] = "return 42" };
            UnityAssetsScriptLoader loader = new(scripts);

            object result = loader.LoadFile("example.lua", new Table(new Script()));

            Assert.That(result, Is.EqualTo("return 42"));
        }

        [Test]
        public void LoadFileNormalizesPathSeparators()
        {
            Dictionary<string, string> scripts = new() { ["path.lua"] = "return 'ok'" };
            UnityAssetsScriptLoader loader = new(scripts);

            object result = loader.LoadFile("foo\\bar/path.lua", new Table(new Script()));

            Assert.That(result, Is.EqualTo("return 'ok'"));
        }

        [Test]
        public void LoadFileThrowsWhenMissing()
        {
            UnityAssetsScriptLoader loader = new(new Dictionary<string, string>());

            Assert.That(
                () => loader.LoadFile("missing.lua", new Table(new Script())),
                Throws.Exception.With.Message.Contains("Cannot load script")
            );
        }

        [Test]
        public void ScriptFileExistsRespectsNormalization()
        {
            Dictionary<string, string> scripts = new() { ["script.lua"] = "return true" };
            UnityAssetsScriptLoader loader = new(scripts);

            Assert.Multiple(() =>
            {
                Assert.That(loader.ScriptFileExists("script.lua"), Is.True);
                Assert.That(loader.ScriptFileExists("nested/script.lua"), Is.True);
                Assert.That(loader.ScriptFileExists("missing.lua"), Is.False);
            });
        }

        [Test]
        public void GetLoadedScriptsReturnsSnapshotOfKeys()
        {
            Dictionary<string, string> scripts = new()
            {
                ["a.lua"] = "return 1",
                ["b.lua"] = "return 2",
                ["c.lua"] = "return 3",
            };
            UnityAssetsScriptLoader loader = new(scripts);

            string[] loaded = loader.GetLoadedScripts();

            Assert.That(loaded, Is.EquivalentTo(new[] { "a.lua", "b.lua", "c.lua" }));
        }

        [Test]
        public void ReflectionConstructorSwallowsMissingUnityAssemblies()
        {
            UnityAssetsScriptLoader loader = new(UnityAssetsScriptLoader.DefaultPath);

            Assert.Multiple(() =>
            {
                Assert.That(loader.ScriptFileExists("missing.lua"), Is.False);
                Assert.That(loader.GetLoadedScripts(), Is.Empty);
            });
        }
    }
}
