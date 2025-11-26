namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
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
        public void NormalizeDirectorySeparatorsReturnsOriginalForEmptyString()
        {
            string result = string.Empty.NormalizeDirectorySeparators('/');

            Assert.That(result, Is.SameAs(string.Empty));
        }

        [Test]
        public void NormalizeDirectorySeparatorsReplacesForwardSlashesWhenRequested()
        {
            string result = "mods/scripts/main.lua".NormalizeDirectorySeparators('\\');

            Assert.That(result, Is.EqualTo("mods\\scripts\\main.lua"));
        }

        [Test]
        public void CopyReplacingDirectorySeparatorsThrowsWhenDestinationIsTooSmall()
        {
            const string Source = "mods/scripts";
            char[] destination = new char[Source.Length - 1];

            TestDelegate action = () =>
                PathSpanExtensions.CopyReplacingDirectorySeparators(
                    Source.AsSpan(),
                    destination.AsSpan(),
                    '/'
                );

            Assert.That(
                action,
                Throws
                    .ArgumentException.With.Message.Contains(
                        "Destination span must be at least as long as the source span."
                    )
                    .And.Property(nameof(ArgumentException.ParamName))
                    .EqualTo("destination")
            );
        }

        [Test]
        public void CopyReplacingDirectorySeparatorsReplacesBothSeparatorTypes()
        {
            ReadOnlySpan<char> source = "mods\\scripts/main.lua";
            char[] buffer = new char[source.Length];

            PathSpanExtensions.CopyReplacingDirectorySeparators(source, buffer.AsSpan(), '/');

            Assert.That(new string(buffer), Is.EqualTo("mods/scripts/main.lua"));
        }
    }
}
