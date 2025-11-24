namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.Utilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PathSpanExtensionsTests
    {
        [Test]
        public void SliceAfterLastSeparatorReturnsOriginalWhenPathHasNoSeparator()
        {
            const string file = "script.lua";

            Assert.That(file.SliceAfterLastSeparator(), Is.SameAs(file));
        }

        [Test]
        public void SliceAfterLastSeparatorReturnsSuffixForUnixPath()
        {
            string result = "/mods/scripts/main.lua".SliceAfterLastSeparator();

            Assert.That(result, Is.EqualTo("main.lua"));
        }

        [Test]
        public void SliceAfterLastSeparatorReturnsSuffixForWindowsPath()
        {
            string result = "C:\\mods\\scripts\\main.lua".SliceAfterLastSeparator();

            Assert.That(result, Is.EqualTo("main.lua"));
        }

        [Test]
        public void NormalizeDirectorySeparatorsReturnsOriginalWhenNothingToReplace()
        {
            const string path = "/scripts/main.lua";

            Assert.That(path.NormalizeDirectorySeparators('/'), Is.SameAs(path));
        }

        [Test]
        public void NormalizeDirectorySeparatorsReplacesBackslashes()
        {
            string result = "mods\\scripts\\main.lua".NormalizeDirectorySeparators('/');

            Assert.That(result, Is.EqualTo("mods/scripts/main.lua"));
        }

        [Test]
        public void NormalizeDirectorySeparatorsReplacesForwardSlashesWhenRequested()
        {
            string result = "mods/scripts/main.lua".NormalizeDirectorySeparators('\\');

            Assert.That(result, Is.EqualTo("mods\\scripts\\main.lua"));
        }
    }
}
