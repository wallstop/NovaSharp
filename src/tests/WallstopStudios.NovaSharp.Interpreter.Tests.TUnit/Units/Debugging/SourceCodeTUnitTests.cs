namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Debugging
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class SourceCodeTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LinesIncludeSyntheticHeaderAndOriginalCode(
            LuaCompatibilityVersion version
        )
        {
            const string ChunkName = "units/source/header";
            const string Code = "local one = 1\nreturn one";
            Script script = new(version);

            script.DoString(Code, null, ChunkName);
            SourceCode source = script.GetSourceCode(script.SourceCodeCount - 1);

            await Assert
                .That(source.Lines[0])
                .IsEqualTo($"-- Begin of chunk : {ChunkName} ")
                .ConfigureAwait(false);
            await Assert.That(source.Lines[1]).IsEqualTo("local one = 1").ConfigureAwait(false);
            await Assert.That(source.Lines[2]).IsEqualTo("return one").ConfigureAwait(false);
            await Assert.That(source.OwnerScript).IsSameReferenceAs(script).ConfigureAwait(false);
            await Assert.That(source.Name).IsEqualTo(ChunkName).ConfigureAwait(false);
            await Assert.That(source.Code).IsEqualTo(Code).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task GetCodeSnippetThrowsWhenSourceRefIsNull(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            script.DoString("return 1", null, "units/source/null");
            SourceCode source = script.GetSourceCode(script.SourceCodeCount - 1);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                source.GetCodeSnippet(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("sourceCodeRef").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task GetCodeSnippetAppendsIntermediateLines(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            script.DoString(
                "local one = 1\nlocal two = one + 1\nlocal three = two + 1\nreturn three",
                null,
                "units/source/multi"
            );
            SourceCode source = script.GetSourceCode(script.SourceCodeCount - 1);

            SourceRef span = new(
                source.SourceId,
                from: 0,
                to: source.Lines[3].Length - 1,
                fromline: 1,
                toline: 3,
                isStepStop: false
            );

            string snippet = source.GetCodeSnippet(span);
            string expected = string.Concat(source.Lines[1], source.Lines[2], source.Lines[3]);

            await Assert.That(snippet).IsEqualTo(expected).ConfigureAwait(false);
        }
    }
}
