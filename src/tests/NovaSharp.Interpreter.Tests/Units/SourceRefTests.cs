namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NUnit.Framework;

    [TestFixture]
    public sealed class SourceRefTests
    {
        [Test]
        public void FormatLocationSwitchesBetweenLuaAndDetailedFormats()
        {
            Script script = new();
            script.LoadString("return 1", null, "chunk");
            int sourceIdx = script.SourceCodeCount - 1;

            SourceRef singleLine = new(sourceIdx, 2, 4, 5, 5, false);
            SourceRef multiLine = new(sourceIdx, 1, 3, 4, 6, false);

            script.Options.UseLuaErrorLocations = false;
            Assert.That(singleLine.FormatLocation(script), Is.EqualTo("chunk:(5,2-4)"));
            Assert.That(multiLine.FormatLocation(script), Is.EqualTo("chunk:(4,1-6,3)"));

            script.Options.UseLuaErrorLocations = true;
            Assert.That(singleLine.FormatLocation(script), Is.EqualTo("chunk:5"));

            Assert.That(
                multiLine.FormatLocation(script, forceClassicFormat: true),
                Is.EqualTo("chunk:4")
            );
        }

        [Test]
        public void GetLocationDistanceHandlesInsideOutsideAndDifferentSources()
        {
            SourceRef sref = new(5, 1, 3, 4, 6, false);

            Assert.That(sref.GetLocationDistance(5, 5, 2), Is.EqualTo(0));
            Assert.That(sref.GetLocationDistance(5, 4, 0), Is.EqualTo(1));
            Assert.That(sref.GetLocationDistance(5, 6, 10), Is.EqualTo(7));
            Assert.That(sref.GetLocationDistance(6, 5, 2), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void IncludesLocationRespectsLineAndColumnBoundaries()
        {
            SourceRef sref = new(7, 1, 3, 4, 6, false);

            Assert.That(sref.IncludesLocation(7, 4, 2), Is.True);
            Assert.That(sref.IncludesLocation(7, 4, 0), Is.False);
            Assert.That(sref.IncludesLocation(7, 6, 3), Is.True);
            Assert.That(sref.IncludesLocation(7, 6, 5), Is.False);
            Assert.That(sref.IncludesLocation(3, 5, 2), Is.False);
        }

        [Test]
        public void SetNoBreakPointAndBreakpointToggleWork()
        {
            SourceRef sref = new(0, 1, 1, 1, 1, false);
            sref.Breakpoint = true;
            SourceRef returned = sref.SetNoBreakPoint();

            Assert.Multiple(() =>
            {
                Assert.That(returned, Is.SameAs(sref));
                Assert.That(sref.Breakpoint, Is.True);
                Assert.That(sref.CannotBreakpoint, Is.True);
            });
        }

        [Test]
        public void ClrLocationAndCopyConstructorPreserveMetadata()
        {
            SourceRef clr = SourceRef.GetClrLocation();
            Assert.That(clr.IsClrLocation, Is.True);
            Assert.That(clr.FormatLocation(new Script()), Is.EqualTo("[clr]"));

            SourceRef original = new(2, 3, 5, 7, 7, false) { Breakpoint = true };
            SourceRef copy = new(original, true);

            Assert.Multiple(() =>
            {
                Assert.That(copy.SourceIdx, Is.EqualTo(2));
                Assert.That(copy.IsStepStop, Is.True);
                Assert.That(copy.Breakpoint, Is.False);
            });
        }

        [Test]
        public void ToStringReflectsStepStopIndicator()
        {
            SourceRef stepStop = new(3, 0, 2, 4, 5, true);

            string representation = stepStop.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(representation, Does.Contain("[3]*"));
                Assert.That(representation, Does.Contain("(4, 0) -> (5, 2)"));
            });
        }

        [Test]
        public void GetLocationDistanceHandlesCollapsedAndBetweenSegments()
        {
            SourceRef collapsed = new(1, 2, 2, 10, 10, false);
            Assert.That(collapsed.GetLocationDistance(1, 10, 2), Is.EqualTo(0));
            Assert.That(collapsed.GetLocationDistance(1, 12, 5), Is.EqualTo(3200));

            SourceRef multi = new(2, 1, 3, 4, 8, false);
            Assert.That(multi.GetLocationDistance(2, 6, 2), Is.EqualTo(0));
            Assert.That(multi.GetLocationDistance(2, 3, 5), Is.EqualTo(1600));
            Assert.That(multi.GetLocationDistance(2, 9, 1), Is.EqualTo(1600));
            Assert.That(multi.GetLocationDistance(2, 8, 1), Is.EqualTo(0));
        }

        [Test]
        public void IncludesLocationHandlesSingleLineSourceRefs()
        {
            SourceRef singleLine = new(4, 0, 2, 5, 5, false);

            Assert.Multiple(() =>
            {
                Assert.That(singleLine.IncludesLocation(4, 5, 1), Is.True);
                Assert.That(singleLine.IncludesLocation(4, 5, 3), Is.False);
            });
        }

        [Test]
        public void FormatLocationUsesPointFormattingWhenCharsMatch()
        {
            Script script = new();
            script.LoadString("return 1", null, "point");
            int sourceIdx = script.SourceCodeCount - 1;
            SourceRef point = new(sourceIdx, 2, 2, 4, 4, false);

            script.Options.UseLuaErrorLocations = false;
            Assert.That(point.FormatLocation(script), Is.EqualTo("point:(4,2)"));
        }
    }
}
