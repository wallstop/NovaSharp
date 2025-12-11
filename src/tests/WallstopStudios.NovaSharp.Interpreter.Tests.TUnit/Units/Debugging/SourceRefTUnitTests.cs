namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Debugging
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;

    public sealed class SourceRefTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task FormatLocationSwitchesBetweenLuaAndDetailedFormats()
        {
            Script script = new();
            script.LoadString("return 1", null, "chunk");
            int sourceIdx = script.SourceCodeCount - 1;

            SourceRef singleLine = new(sourceIdx, 2, 4, 5, 5, false);
            SourceRef multiLine = new(sourceIdx, 1, 3, 4, 6, false);

            script.Options.UseLuaErrorLocations = false;
            await Assert
                .That(singleLine.FormatLocation(script))
                .IsEqualTo("chunk:(5,2-4)")
                .ConfigureAwait(false);
            await Assert
                .That(multiLine.FormatLocation(script))
                .IsEqualTo("chunk:(4,1-6,3)")
                .ConfigureAwait(false);

            script.Options.UseLuaErrorLocations = true;
            await Assert
                .That(singleLine.FormatLocation(script))
                .IsEqualTo("chunk:5")
                .ConfigureAwait(false);
            await Assert
                .That(multiLine.FormatLocation(script, forceClassicFormat: true))
                .IsEqualTo("chunk:4")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetLocationDistanceHandlesInsideOutsideAndDifferentSources()
        {
            SourceRef sref = new(5, 1, 3, 4, 6, false);

            await Assert.That(sref.GetLocationDistance(5, 5, 2)).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(sref.GetLocationDistance(5, 4, 0)).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(sref.GetLocationDistance(5, 6, 10))
                .IsEqualTo(7)
                .ConfigureAwait(false);
            await Assert
                .That(sref.GetLocationDistance(6, 5, 2))
                .IsEqualTo(int.MaxValue)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IncludesLocationRespectsLineAndColumnBoundaries()
        {
            SourceRef sref = new(7, 1, 3, 4, 6, false);

            await Assert.That(sref.IncludesLocation(7, 4, 2)).IsTrue().ConfigureAwait(false);
            await Assert.That(sref.IncludesLocation(7, 4, 0)).IsFalse().ConfigureAwait(false);
            await Assert.That(sref.IncludesLocation(7, 6, 3)).IsTrue().ConfigureAwait(false);
            await Assert.That(sref.IncludesLocation(7, 6, 5)).IsFalse().ConfigureAwait(false);
            await Assert.That(sref.IncludesLocation(3, 5, 2)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetNoBreakPointAndBreakpointToggleWork()
        {
            SourceRef sref = new(0, 1, 1, 1, 1, false) { Breakpoint = true };

            SourceRef returned = sref.SetNoBreakPoint();

            await Assert
                .That(object.ReferenceEquals(returned, sref))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(sref.Breakpoint).IsTrue().ConfigureAwait(false);
            await Assert.That(sref.CannotBreakpoint).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ClrLocationAndCopyConstructorPreserveMetadata()
        {
            SourceRef clr = SourceRef.GetClrLocation();
            await Assert.That(clr.IsClrLocation).IsTrue().ConfigureAwait(false);
            await Assert
                .That(clr.FormatLocation(new Script()))
                .IsEqualTo("[clr]")
                .ConfigureAwait(false);

            SourceRef original = new(2, 3, 5, 7, 7, false) { Breakpoint = true };
            SourceRef copy = new(original, true);

            await Assert.That(copy.SourceIdx).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(copy.IsStepStop).IsTrue().ConfigureAwait(false);
            await Assert.That(copy.Breakpoint).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToStringReflectsStepStopIndicator()
        {
            SourceRef stepStop = new(3, 0, 2, 4, 5, true);
            SourceRef regular = new(3, 0, 2, 4, 5, false);

            string representation = stepStop.ToString();
            string nonStepRepresentation = regular.ToString();

            await Assert.That(representation).Contains("[3]*").ConfigureAwait(false);
            await Assert.That(representation).Contains("(4, 0) -> (5, 2)").ConfigureAwait(false);
            await Assert.That(nonStepRepresentation).Contains("[3] ").ConfigureAwait(false);
            await Assert
                .That(nonStepRepresentation)
                .Contains("(4, 0) -> (5, 2)")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetLocationDistanceHandlesCollapsedAndBetweenSegments()
        {
            SourceRef collapsed = new(1, 2, 2, 10, 10, false);

            await Assert
                .That(collapsed.GetLocationDistance(1, 10, 2))
                .IsEqualTo(0)
                .ConfigureAwait(false);
            await Assert
                .That(collapsed.GetLocationDistance(1, 12, 5))
                .IsEqualTo(3200)
                .ConfigureAwait(false);

            SourceRef multi = new(2, 1, 3, 4, 8, false);
            await Assert
                .That(multi.GetLocationDistance(2, 6, 2))
                .IsEqualTo(0)
                .ConfigureAwait(false);
            await Assert
                .That(multi.GetLocationDistance(2, 3, 5))
                .IsEqualTo(1600)
                .ConfigureAwait(false);
            await Assert
                .That(multi.GetLocationDistance(2, 9, 1))
                .IsEqualTo(1600)
                .ConfigureAwait(false);
            await Assert
                .That(multi.GetLocationDistance(2, 8, 1))
                .IsEqualTo(0)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetLocationDistanceHandlesSingleLineOffsets()
        {
            SourceRef singleLine = new(9, 3, 5, 12, 12, false);

            await Assert
                .That(singleLine.GetLocationDistance(9, 12, 2))
                .IsEqualTo(1)
                .ConfigureAwait(false);
            await Assert
                .That(singleLine.GetLocationDistance(9, 12, 8))
                .IsEqualTo(3)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IncludesLocationHandlesSingleLineSourceRefs()
        {
            SourceRef singleLine = new(4, 0, 2, 5, 5, false);

            await Assert.That(singleLine.IncludesLocation(4, 5, 1)).IsTrue().ConfigureAwait(false);
            await Assert.That(singleLine.IncludesLocation(4, 5, 3)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IncludesLocationReturnsTrueForMiddleLines()
        {
            SourceRef multi = new(10, 1, 3, 4, 7, false);

            await Assert.That(multi.IncludesLocation(10, 6, 0)).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatLocationUsesPointFormattingWhenCharsMatch()
        {
            Script script = new();
            script.LoadString("return 1", null, "point");
            int sourceIdx = script.SourceCodeCount - 1;
            SourceRef point = new(sourceIdx, 2, 2, 4, 4, false);

            script.Options.UseLuaErrorLocations = false;
            await Assert
                .That(point.FormatLocation(script))
                .IsEqualTo("point:(4,2)")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatLocationThrowsWhenScriptIsNull()
        {
            SourceRef point = new(0, 0, 0, 0, 0, false);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                point.FormatLocation(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("script").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CopyConstructorThrowsWhenSourceIsNull()
        {
            SourceRef result = null;
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                result = new SourceRef(null, false)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("src").ConfigureAwait(false);
        }
    }
}
