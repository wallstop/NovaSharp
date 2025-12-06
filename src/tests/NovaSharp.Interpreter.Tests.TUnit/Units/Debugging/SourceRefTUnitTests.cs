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

            await Assert.That(snippet).IsEqualTo("sample").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCodeSnippetClampsOutOfRangeIndices()
        {
            (_, SourceCode source) = CreateScript("return 123", "units/snippet_clamp");
            SourceRef span = new(source.SourceId, -10, 400, 1, 1, false);

            string snippet = source.GetCodeSnippet(span);

            await Assert.That(snippet).IsEqualTo("return 123").ConfigureAwait(false);
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

            await Assert.That(snippet).IsEqualTo(expected).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IncludesLocationHonoursBounds()
        {
            (_, SourceCode source) = CreateScript(
                "local flag = true\nflag = not flag",
                "units/includes"
            );

            SourceRef span = new(source.SourceId, 2, 6, 1, 2, false);

            await Assert
                .That(span.IncludesLocation(source.SourceId, 1, 2))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(span.IncludesLocation(source.SourceId, 1, 1))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(span.IncludesLocation(source.SourceId, 2, 6))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(span.IncludesLocation(source.SourceId, 2, 7))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(span.IncludesLocation(source.SourceId + 1, 1, 2))
                .IsFalse()
                .ConfigureAwait(false);
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
                .IsEqualTo(int.MaxValue)
                .ConfigureAwait(false);
            await Assert
                .That(span.GetLocationDistance(source.SourceId, 1, 3))
                .IsEqualTo(1)
                .ConfigureAwait(false);
            await Assert
                .That(span.GetLocationDistance(source.SourceId, 1, 6))
                .IsEqualTo(0)
                .ConfigureAwait(false);
            await Assert
                .That(span.GetLocationDistance(source.SourceId, 2, 10))
                .IsEqualTo(0)
                .ConfigureAwait(false);
            await Assert
                .That(span.GetLocationDistance(source.SourceId, 3, 11))
                .IsEqualTo(3)
                .ConfigureAwait(false);
            await Assert
                .That(span.GetLocationDistance(source.SourceId, 0, 0))
                .IsEqualTo(1600)
                .ConfigureAwait(false);
            await Assert
                .That(span.GetLocationDistance(source.SourceId, 4, 0))
                .IsEqualTo(1600)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatLocationRespectsScriptOptions()
        {
            (Script script, SourceCode source) = CreateScript("return 42", "units/format");
            SourceRef location = new(source.SourceId, 2, 2, 1, 1, false);

            script.Options.UseLuaErrorLocations = true;
            await Assert
                .That(location.FormatLocation(script))
                .IsEqualTo("units/format:1")
                .ConfigureAwait(false);

            script.Options.UseLuaErrorLocations = false;
            await Assert
                .That(location.FormatLocation(script))
                .IsEqualTo("units/format:(1,2)")
                .ConfigureAwait(false);

            SourceRef span = new(source.SourceId, 0, 5, 1, 1, false);
            await Assert
                .That(span.FormatLocation(script))
                .IsEqualTo("units/format:(1,0-5)")
                .ConfigureAwait(false);

            SourceRef multi = new(source.SourceId, 0, 3, 1, 2, false);
            await Assert
                .That(multi.FormatLocation(script))
                .IsEqualTo("units/format:(1,0-2,3)")
                .ConfigureAwait(false);

            await Assert
                .That(location.FormatLocation(script, forceClassicFormat: true))
                .IsEqualTo("units/format:1")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatLocationReturnsClrMarkerForClrLocations()
        {
            Script script = new();
            SourceRef location = SourceRef.GetClrLocation();

            await Assert
                .That(location.FormatLocation(script))
                .IsEqualTo("[clr]")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetNoBreakPointFlagsSourceRef()
        {
            SourceRef span = new(0, 0, 0, 0, 0, false);

            SourceRef returned = span.SetNoBreakPoint();

            await Assert.That(span.CannotBreakpoint).IsTrue().ConfigureAwait(false);
            await Assert
                .That(object.ReferenceEquals(returned, span))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CopyConstructorCopiesCoordinatesAndStepFlag()
        {
            SourceRef original = new(1, 0, 4, 2, 3, false) { Breakpoint = true };
            SourceRef copy = new(original, true);

            await Assert.That(copy.SourceIdx).IsEqualTo(original.SourceIdx).ConfigureAwait(false);
            await Assert.That(copy.FromChar).IsEqualTo(original.FromChar).ConfigureAwait(false);
            await Assert.That(copy.ToChar).IsEqualTo(original.ToChar).ConfigureAwait(false);
            await Assert.That(copy.FromLine).IsEqualTo(original.FromLine).ConfigureAwait(false);
            await Assert.That(copy.ToLine).IsEqualTo(original.ToLine).ConfigureAwait(false);
            await Assert.That(copy.IsStepStop).IsTrue().ConfigureAwait(false);
            await Assert.That(copy.Breakpoint).IsFalse().ConfigureAwait(false);
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
