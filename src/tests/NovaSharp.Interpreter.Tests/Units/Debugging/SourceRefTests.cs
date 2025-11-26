namespace NovaSharp.Interpreter.Tests.Units.Debugging
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NUnit.Framework;

    [TestFixture]
    public sealed class SourceRefTests
    {
        [Test]
        public void GetCodeSnippetReturnsRequestedSegmentWithinSingleLine()
        {
            (_, SourceCode source) = CreateScript("local sample = 42", "units/snippet_single");
            SourceRef span = new(source.SourceId, 6, 12, 1, 1, false);

            string snippet = source.GetCodeSnippet(span);

            Assert.That(snippet, Is.EqualTo("sample"));
        }

        [Test]
        public void GetCodeSnippetClampsOutOfRangeIndices()
        {
            (_, SourceCode source) = CreateScript("return 123", "units/snippet_clamp");
            SourceRef span = new(source.SourceId, -10, 400, 1, 1, false);

            string snippet = source.GetCodeSnippet(span);

            Assert.That(snippet, Is.EqualTo("return 123"));
        }

        [Test]
        public void GetCodeSnippetAggregatesMultiLineSegments()
        {
            (_, SourceCode source) = CreateScript(
                "local one = 1\nlocal two = one + 1",
                "units/snippet_multi"
            );

            int finalLineLength = source.Lines[2].Length;
            SourceRef span = new(source.SourceId, 6, finalLineLength - 1, 1, 2, false);

            string snippet = source.GetCodeSnippet(span);

            string expected = string.Concat(source.Lines[1].AsSpan(6), source.Lines[2]);
            Assert.That(snippet, Is.EqualTo(expected));
        }

        [Test]
        public void IncludesLocationHonoursBounds()
        {
            (_, SourceCode source) = CreateScript(
                "local flag = true\nflag = not flag",
                "units/includes"
            );

            SourceRef span = new(source.SourceId, 2, 6, 1, 2, false);

            Assert.Multiple(() =>
            {
                Assert.That(span.IncludesLocation(source.SourceId, 1, 2), Is.True);
                Assert.That(span.IncludesLocation(source.SourceId, 1, 1), Is.False);
                Assert.That(span.IncludesLocation(source.SourceId, 2, 6), Is.True);
                Assert.That(span.IncludesLocation(source.SourceId, 2, 7), Is.False);
                Assert.That(span.IncludesLocation(source.SourceId + 1, 1, 2), Is.False);
            });
        }

        [Test]
        public void GetLocationDistanceComputesHeuristicOffsets()
        {
            (_, SourceCode source) = CreateScript(
                "local one = 1\nlocal two = one + 1\nreturn two",
                "units/distance"
            );

            SourceRef span = new(source.SourceId, 4, 8, 1, 3, false);

            Assert.Multiple(() =>
            {
                Assert.That(
                    span.GetLocationDistance(source.SourceId + 1, 1, 5),
                    Is.EqualTo(int.MaxValue)
                );
                Assert.That(span.GetLocationDistance(source.SourceId, 1, 3), Is.EqualTo(1)); // before start col
                Assert.That(span.GetLocationDistance(source.SourceId, 1, 6), Is.EqualTo(0)); // inside start line
                Assert.That(span.GetLocationDistance(source.SourceId, 2, 10), Is.EqualTo(0)); // middle lines
                Assert.That(span.GetLocationDistance(source.SourceId, 3, 11), Is.EqualTo(3)); // after end col
                Assert.That(span.GetLocationDistance(source.SourceId, 0, 0), Is.EqualTo(1600)); // previous line
                Assert.That(span.GetLocationDistance(source.SourceId, 4, 0), Is.EqualTo(1600)); // following line
            });
        }

        [Test]
        public void FormatLocationRespectsScriptOptions()
        {
            (Script script, SourceCode source) = CreateScript("return 42", "units/format");
            SourceRef location = new(source.SourceId, 2, 2, 1, 1, false);

            script.Options.UseLuaErrorLocations = true;
            Assert.That(location.FormatLocation(script), Is.EqualTo("units/format:1"));

            script.Options.UseLuaErrorLocations = false;
            Assert.That(location.FormatLocation(script), Is.EqualTo("units/format:(1,2)"));

            SourceRef span = new(source.SourceId, 0, 5, 1, 1, false);
            Assert.That(span.FormatLocation(script), Is.EqualTo("units/format:(1,0-5)"));

            SourceRef multi = new(source.SourceId, 0, 3, 1, 2, false);
            Assert.That(multi.FormatLocation(script), Is.EqualTo("units/format:(1,0-2,3)"));

            Assert.That(
                location.FormatLocation(script, forceClassicFormat: true),
                Is.EqualTo("units/format:1")
            );
        }

        [Test]
        public void FormatLocationReturnsClrMarkerForClrLocations()
        {
            Script script = new();
            SourceRef location = SourceRef.GetClrLocation();

            Assert.That(location.FormatLocation(script), Is.EqualTo("[clr]"));
        }

        [Test]
        public void SetNoBreakPointFlagsSourceRef()
        {
            SourceRef span = new(0, 0, 0, 0, 0, false);

            SourceRef returned = span.SetNoBreakPoint();

            Assert.Multiple(() =>
            {
                Assert.That(span.CannotBreakpoint, Is.True);
                Assert.That(returned, Is.SameAs(span));
            });
        }

        [Test]
        public void CopyConstructorCopiesCoordinatesAndStepFlag()
        {
            SourceRef original = new(1, 0, 4, 2, 3, false) { Breakpoint = true };
            SourceRef copy = new(original, true);

            Assert.Multiple(() =>
            {
                Assert.That(copy.SourceIdx, Is.EqualTo(original.SourceIdx));
                Assert.That(copy.FromChar, Is.EqualTo(original.FromChar));
                Assert.That(copy.ToChar, Is.EqualTo(original.ToChar));
                Assert.That(copy.FromLine, Is.EqualTo(original.FromLine));
                Assert.That(copy.ToLine, Is.EqualTo(original.ToLine));
                Assert.That(copy.IsStepStop, Is.True);
                Assert.That(copy.Breakpoint, Is.False); // copy constructor does not copy breakpoint state
            });
        }

        private static (Script Script, SourceCode Source) CreateScript(string code, string name)
        {
            Script script = new();
            script.DoString(code, null, name);
            SourceCode source = script.GetSourceCode(script.SourceCodeCount - 1);
            return (script, source);
        }
    }
}
