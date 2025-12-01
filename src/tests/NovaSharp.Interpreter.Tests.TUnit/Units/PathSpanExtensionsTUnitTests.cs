#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Utilities;

    public sealed class PathSpanExtensionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task SliceAfterLastSeparatorReturnsOriginalWhenPathHasNoSeparator()
        {
            const string file = "script.lua";

            string result = file.SliceAfterLastSeparator();

            await Assert.That(object.ReferenceEquals(result, file)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SliceAfterLastSeparatorReturnsSuffixForUnixPath()
        {
            string result = "/mods/scripts/main.lua".SliceAfterLastSeparator();

            await Assert.That(result).IsEqualTo("main.lua");
        }

        [global::TUnit.Core.Test]
        public async Task SliceAfterLastSeparatorReturnsSuffixForWindowsPath()
        {
            string result = "C:\\mods\\scripts\\main.lua".SliceAfterLastSeparator();

            await Assert.That(result).IsEqualTo("main.lua");
        }

        [global::TUnit.Core.Test]
        public async Task NormalizeDirectorySeparatorsReturnsOriginalWhenNothingToReplace()
        {
            const string path = "/scripts/main.lua";

            string result = path.NormalizeDirectorySeparators('/');

            await Assert.That(object.ReferenceEquals(result, path)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task NormalizeDirectorySeparatorsReplacesBackslashes()
        {
            string result = "mods\\scripts\\main.lua".NormalizeDirectorySeparators('/');

            await Assert.That(result).IsEqualTo("mods/scripts/main.lua");
        }

        [global::TUnit.Core.Test]
        public async Task NormalizeDirectorySeparatorsReturnsOriginalForEmptyString()
        {
            string result = string.Empty.NormalizeDirectorySeparators('/');

            await Assert.That(object.ReferenceEquals(result, string.Empty)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task NormalizeDirectorySeparatorsReplacesForwardSlashesWhenRequested()
        {
            string result = "mods/scripts/main.lua".NormalizeDirectorySeparators('\\');

            await Assert.That(result).IsEqualTo("mods\\scripts\\main.lua");
        }

        [global::TUnit.Core.Test]
        public async Task CopyReplacingDirectorySeparatorsThrowsWhenDestinationIsTooSmall()
        {
            const string Source = "mods/scripts";
            char[] destination = new char[Source.Length - 1];

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                PathSpanExtensions.CopyReplacingDirectorySeparators(
                    Source.AsSpan(),
                    destination.AsSpan(),
                    '/'
                )
            )!;

            await Assert
                .That(exception.Message)
                .Contains("Destination span must be at least as long as the source span.");
            await Assert.That(exception.ParamName).IsEqualTo("destination");
        }

        [global::TUnit.Core.Test]
        public async Task CopyReplacingDirectorySeparatorsReplacesBothSeparatorTypes()
        {
            const string Source = "mods\\scripts/main.lua";
            string result = CopyReplacingDirectorySeparators(Source, '/');

            await Assert.That(result).IsEqualTo("mods/scripts/main.lua");
        }

        private static string CopyReplacingDirectorySeparators(string source, char separator)
        {
            char[] destination = new char[source.Length];
            PathSpanExtensions.CopyReplacingDirectorySeparators(
                source.AsSpan(),
                destination.AsSpan(),
                separator
            );
            return new string(destination);
        }
    }
}
#pragma warning restore CA2007
