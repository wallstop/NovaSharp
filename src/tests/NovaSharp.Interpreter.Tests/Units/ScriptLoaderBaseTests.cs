namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptLoaderBaseTests
    {
        [Test]
        public void UnpackStringPathsSplitsAndTrimsSegments()
        {
            IReadOnlyList<string> segments = ScriptLoaderBase.UnpackStringPaths(
                " ? ;?.lua ; ./?.txt ;  "
            );
            List<string> expected = new() { "?", "?.lua", "./?.txt" };

            Assert.That(segments, Is.EqualTo(expected));
        }

        [Test]
        public void UnpackStringPathsThrowsWhenNull()
        {
            Assert.That(
                () => ScriptLoaderBase.UnpackStringPaths(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("str")
            );
        }

        [Test]
        public void ResolveModuleNameFromPathsReturnsFirstMatchingFile()
        {
            TestScriptLoader loader = new();
            loader.AddExisting("modules/foo.lua");

            string resolved = loader.ResolveFromPaths("foo", "modules/?.lua", "fallback/?.lua");

            Assert.That(resolved, Is.EqualTo("modules/foo.lua"));
        }

        [Test]
        public void ResolveModuleNameReturnsNullWhenNoPathsMatch()
        {
            TestScriptLoader loader = new();

            string resolved = loader.ResolveFromPaths("missing", Array.Empty<string>());

            Assert.That(resolved, Is.Null);
        }

        private sealed class TestScriptLoader : ScriptLoaderBase
        {
            private readonly HashSet<string> _existing = new(StringComparer.Ordinal);

            public override bool ScriptFileExists(string name)
            {
                return _existing.Contains(name);
            }

            public override object LoadFile(string file, Table globalContext)
            {
                throw new NotImplementedException();
            }

            public void AddExisting(string path)
            {
                _existing.Add(path);
            }

            public string ResolveFromPaths(string moduleName, params string[] paths)
            {
                return ResolveModuleName(moduleName, paths);
            }
        }
    }
}
