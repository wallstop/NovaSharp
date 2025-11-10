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
        public void LoadFileReturnsScriptFromResourceMap()
        {
            Dictionary<string, string> scripts = new Dictionary<string, string>
            {
                { "init.lua", "return 42" },
            };

            UnityAssetsScriptLoader loader = new UnityAssetsScriptLoader(scripts);
            object content = loader.LoadFile("init.lua", null);

            Assert.That(content, Is.EqualTo("return 42"));
        }

        [Test]
        public void LoadFileTrimsDirectorySegments()
        {
            Dictionary<string, string> scripts = new Dictionary<string, string>
            {
                { "config.lua", "value = 10" },
            };

            UnityAssetsScriptLoader loader = new UnityAssetsScriptLoader(scripts);
            object content = loader.LoadFile("Assets/Resources/NovaSharp/Scripts/config.lua", null);

            Assert.That(content, Is.EqualTo("value = 10"));
        }

        [Test]
        public void LoadFileThrowsWhenScriptMissing()
        {
            UnityAssetsScriptLoader loader = new UnityAssetsScriptLoader(
                new Dictionary<string, string>()
            );

            Assert.That(
                () => loader.LoadFile("missing.lua", null),
                Throws.TypeOf<Exception>().With.Message.Contain(UnityAssetsScriptLoader.DefaultPath)
            );
        }

        [Test]
        public void ScriptFileExistsMatchesStoredNames()
        {
            Dictionary<string, string> scripts = new Dictionary<string, string>
            {
                { "main.lua", "print('hello')" },
            };

            UnityAssetsScriptLoader loader = new UnityAssetsScriptLoader(scripts);

            Assert.Multiple(() =>
            {
                Assert.That(loader.ScriptFileExists("main.lua"), Is.True);
                Assert.That(loader.ScriptFileExists("Assets/Resources/main.lua"), Is.True);
                Assert.That(loader.ScriptFileExists("other.lua"), Is.False);
            });
        }

        [Test]
        public void GetLoadedScriptsReturnsMappedKeys()
        {
            Dictionary<string, string> scripts = new Dictionary<string, string>
            {
                { "one.lua", "x = 1" },
                { "two.lua", "x = 2" },
            };

            UnityAssetsScriptLoader loader = new UnityAssetsScriptLoader(scripts);
            string[] loaded = loader.GetLoadedScripts();

            Assert.That(loaded, Is.EquivalentTo(new[] { "one.lua", "two.lua" }));
        }
    }
}
