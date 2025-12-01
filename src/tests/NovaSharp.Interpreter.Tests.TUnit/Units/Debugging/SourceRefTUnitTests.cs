#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units.Debugging
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;

    public sealed class SourceRefTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GetCodeSnippetReturnsRequestedSegmentWithinSingleLine()
        {
            (_, SourceCode source) = CreateScript("local sample = 42", "units/snippet_single");
            SourceRef span = new(source.SourceId, 6, 12, 1, 1, false);

            string snippet = source.GetCodeSnippet(span);

            await Assert.That(snippet).IsEqualTo("sample");
        }

        [global::TUnit.Core.Test]
        public async Task GetCodeSnippetClampsOutOfRangeIndices()
        {
            (_, SourceCode source) = CreateScript("return 123", "units/snippet_clamp");
            SourceRef span = new(source.SourceId, -10, 400, 1, 1, false);

            string snippet = source.GetCodeSnippet(span);

            await Assert.That(snippet).IsEqualTo("return 123");
        }

        [global::TUnit.Core.Test]
        public async Task GetCodeSnippetAggregatesMultiLineSegments()
        {
            (_, SourceCode source) = CreateScript(
                "local one = 1\nlocal two = one + 1",
                "units/snippet_multi"
            );

            int finalLineLength = source.Lines[2].Length;
            SourceRef span = new(source.SourceId, 6, finalLineLength - 1, 1, 2, false);

            string snippet = source.GetCodeSnippet(span);
            string expected = string.Concat(source.Lines[1].AsSpan(6), source.Lines[2]);

            await Assert.That(snippet).IsEqualTo(expected);
        }

        [global::TUnit.Core.Test]
        public async Task IncludesLocationHonoursBounds()
        {
            (_, SourceCode source) = CreateScript(
                "local flag = true\nflag = not flag",
                "units/includes"
            );

            SourceRef span = new(source.SourceId, 2, 6, 1, 2, false);

            await Assert.That(span.IncludesLocation(source.SourceId, 1, 2)).IsTrue();
            await Assert.That(span.IncludesLocation(source.SourceId, 1, 1)).IsFalse();
            await Assert.That(span.IncludesLocation(source.SourceId, 2, 6)).IsTrue();
            await Assert.That(span.IncludesLocation(source.SourceId, 2, 7)).IsFalse();
            await Assert.That(span.IncludesLocation(source.SourceId + 1, 1, 2)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task GetLocationDistanceComputesHeuristicOffsets()
        {
            (_, SourceCode source) = CreateScript(
                "local one = 1\nlocal two = one + 1\nreturn two",
                "units/distance"
            );

            SourceRef span = new(source.SourceId, 4, 8, 1, 3, false);

            await Assert
                .That(span.GetLocationDistance(source.SourceId + 1, 1, 5))
                .IsEqualTo(int.MaxValue);
            await Assert.That(span.GetLocationDistance(source.SourceId, 1, 3)).IsEqualTo(1);
            await Assert.That(span.GetLocationDistance(source.SourceId, 1, 6)).IsEqualTo(0);
            await Assert.That(span.GetLocationDistance(source.SourceId, 2, 10)).IsEqualTo(0);
            await Assert.That(span.GetLocationDistance(source.SourceId, 3, 11)).IsEqualTo(3);
            await Assert.That(span.GetLocationDistance(source.SourceId, 0, 0)).IsEqualTo(1600);
            await Assert.That(span.GetLocationDistance(source.SourceId, 4, 0)).IsEqualTo(1600);
        }

        [global::TUnit.Core.Test]
        public async Task FormatLocationRespectsScriptOptions()
        {
            (Script script, SourceCode source) = CreateScript("return 42", "units/format");
            SourceRef location = new(source.SourceId, 2, 2, 1, 1, false);

            script.Options.UseLuaErrorLocations = true;
            await Assert.That(location.FormatLocation(script)).IsEqualTo("units/format:1");

            script.Options.UseLuaErrorLocations = false;
            await Assert.That(location.FormatLocation(script)).IsEqualTo("units/format:(1,2)");

            SourceRef span = new(source.SourceId, 0, 5, 1, 1, false);
            await Assert.That(span.FormatLocation(script)).IsEqualTo("units/format:(1,0-5)");

            SourceRef multi = new(source.SourceId, 0, 3, 1, 2, false);
            await Assert.That(multi.FormatLocation(script)).IsEqualTo("units/format:(1,0-2,3)");

            await Assert
                .That(location.FormatLocation(script, forceClassicFormat: true))
                .IsEqualTo("units/format:1");
        }

        [global::TUnit.Core.Test]
        public async Task FormatLocationReturnsClrMarkerForClrLocations()
        {
            Script script = new();
            SourceRef location = SourceRef.GetClrLocation();

            await Assert.That(location.FormatLocation(script)).IsEqualTo("[clr]");
        }

        [global::TUnit.Core.Test]
        public async Task SetNoBreakPointFlagsSourceRef()
        {
            SourceRef span = new(0, 0, 0, 0, 0, false);

            SourceRef returned = span.SetNoBreakPoint();

            await Assert.That(span.CannotBreakpoint).IsTrue();
            await Assert.That(object.ReferenceEquals(returned, span)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task CopyConstructorCopiesCoordinatesAndStepFlag()
        {
            SourceRef original = new(1, 0, 4, 2, 3, false) { Breakpoint = true };
            SourceRef copy = new(original, true);

            await Assert.That(copy.SourceIdx).IsEqualTo(original.SourceIdx);
            await Assert.That(copy.FromChar).IsEqualTo(original.FromChar);
            await Assert.That(copy.ToChar).IsEqualTo(original.ToChar);
            await Assert.That(copy.FromLine).IsEqualTo(original.FromLine);
            await Assert.That(copy.ToLine).IsEqualTo(original.ToLine);
            await Assert.That(copy.IsStepStop).IsTrue();
            await Assert.That(copy.Breakpoint).IsFalse();
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
#pragma warning restore CA2007
