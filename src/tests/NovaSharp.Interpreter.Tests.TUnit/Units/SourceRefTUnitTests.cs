#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;

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
            await Assert.That(singleLine.FormatLocation(script)).IsEqualTo("chunk:(5,2-4)");
            await Assert.That(multiLine.FormatLocation(script)).IsEqualTo("chunk:(4,1-6,3)");

            script.Options.UseLuaErrorLocations = true;
            await Assert.That(singleLine.FormatLocation(script)).IsEqualTo("chunk:5");
            await Assert
                .That(multiLine.FormatLocation(script, forceClassicFormat: true))
                .IsEqualTo("chunk:4");
        }

        [global::TUnit.Core.Test]
        public async Task GetLocationDistanceHandlesInsideOutsideAndDifferentSources()
        {
            SourceRef sref = new(5, 1, 3, 4, 6, false);

            await Assert.That(sref.GetLocationDistance(5, 5, 2)).IsEqualTo(0);
            await Assert.That(sref.GetLocationDistance(5, 4, 0)).IsEqualTo(1);
            await Assert.That(sref.GetLocationDistance(5, 6, 10)).IsEqualTo(7);
            await Assert.That(sref.GetLocationDistance(6, 5, 2)).IsEqualTo(int.MaxValue);
        }

        [global::TUnit.Core.Test]
        public async Task IncludesLocationRespectsLineAndColumnBoundaries()
        {
            SourceRef sref = new(7, 1, 3, 4, 6, false);

            await Assert.That(sref.IncludesLocation(7, 4, 2)).IsTrue();
            await Assert.That(sref.IncludesLocation(7, 4, 0)).IsFalse();
            await Assert.That(sref.IncludesLocation(7, 6, 3)).IsTrue();
            await Assert.That(sref.IncludesLocation(7, 6, 5)).IsFalse();
            await Assert.That(sref.IncludesLocation(3, 5, 2)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task SetNoBreakPointAndBreakpointToggleWork()
        {
            SourceRef sref = new(0, 1, 1, 1, 1, false) { Breakpoint = true };

            SourceRef returned = sref.SetNoBreakPoint();

            await Assert.That(object.ReferenceEquals(returned, sref)).IsTrue();
            await Assert.That(sref.Breakpoint).IsTrue();
            await Assert.That(sref.CannotBreakpoint).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ClrLocationAndCopyConstructorPreserveMetadata()
        {
            SourceRef clr = SourceRef.GetClrLocation();
            await Assert.That(clr.IsClrLocation).IsTrue();
            await Assert.That(clr.FormatLocation(new Script())).IsEqualTo("[clr]");

            SourceRef original = new(2, 3, 5, 7, 7, false) { Breakpoint = true };
            SourceRef copy = new(original, true);

            await Assert.That(copy.SourceIdx).IsEqualTo(2);
            await Assert.That(copy.IsStepStop).IsTrue();
            await Assert.That(copy.Breakpoint).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ToStringReflectsStepStopIndicator()
        {
            SourceRef stepStop = new(3, 0, 2, 4, 5, true);
            SourceRef regular = new(3, 0, 2, 4, 5, false);

            string representation = stepStop.ToString();
            string nonStepRepresentation = regular.ToString();

            await Assert.That(representation).Contains("[3]*");
            await Assert.That(representation).Contains("(4, 0) -> (5, 2)");
            await Assert.That(nonStepRepresentation).Contains("[3] ");
            await Assert.That(nonStepRepresentation).Contains("(4, 0) -> (5, 2)");
        }

        [global::TUnit.Core.Test]
        public async Task GetLocationDistanceHandlesCollapsedAndBetweenSegments()
        {
            SourceRef collapsed = new(1, 2, 2, 10, 10, false);

            await Assert.That(collapsed.GetLocationDistance(1, 10, 2)).IsEqualTo(0);
            await Assert.That(collapsed.GetLocationDistance(1, 12, 5)).IsEqualTo(3200);

            SourceRef multi = new(2, 1, 3, 4, 8, false);
            await Assert.That(multi.GetLocationDistance(2, 6, 2)).IsEqualTo(0);
            await Assert.That(multi.GetLocationDistance(2, 3, 5)).IsEqualTo(1600);
            await Assert.That(multi.GetLocationDistance(2, 9, 1)).IsEqualTo(1600);
            await Assert.That(multi.GetLocationDistance(2, 8, 1)).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task GetLocationDistanceHandlesSingleLineOffsets()
        {
            SourceRef singleLine = new(9, 3, 5, 12, 12, false);

            await Assert.That(singleLine.GetLocationDistance(9, 12, 2)).IsEqualTo(1);
            await Assert.That(singleLine.GetLocationDistance(9, 12, 8)).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task IncludesLocationHandlesSingleLineSourceRefs()
        {
            SourceRef singleLine = new(4, 0, 2, 5, 5, false);

            await Assert.That(singleLine.IncludesLocation(4, 5, 1)).IsTrue();
            await Assert.That(singleLine.IncludesLocation(4, 5, 3)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task IncludesLocationReturnsTrueForMiddleLines()
        {
            SourceRef multi = new(10, 1, 3, 4, 7, false);

            await Assert.That(multi.IncludesLocation(10, 6, 0)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task FormatLocationUsesPointFormattingWhenCharsMatch()
        {
            Script script = new();
            script.LoadString("return 1", null, "point");
            int sourceIdx = script.SourceCodeCount - 1;
            SourceRef point = new(sourceIdx, 2, 2, 4, 4, false);

            script.Options.UseLuaErrorLocations = false;
            await Assert.That(point.FormatLocation(script)).IsEqualTo("point:(4,2)");
        }

        [global::TUnit.Core.Test]
        public async Task FormatLocationThrowsWhenScriptIsNull()
        {
            SourceRef point = new(0, 0, 0, 0, 0, false);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                point.FormatLocation(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("script");
        }

        [global::TUnit.Core.Test]
        public async Task CopyConstructorThrowsWhenSourceIsNull()
        {
            SourceRef result = null;
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                result = new SourceRef(null, false)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("src");
        }
    }
}
#pragma warning restore CA2007
