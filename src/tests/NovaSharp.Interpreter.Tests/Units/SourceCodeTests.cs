namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NUnit.Framework;

    [TestFixture]
    public sealed class SourceCodeTests
    {
        [Test]
        public void LinesIncludeSyntheticHeaderAndOriginalCode()
        {
            const string ChunkName = "units/source/header";
            const string Code = "local one = 1\nreturn one";
            Script script = new();

            script.DoString(Code, null, ChunkName);
            SourceCode source = script.GetSourceCode(script.SourceCodeCount - 1);

            Assert.Multiple(() =>
            {
                Assert.That(source.Lines[0], Is.EqualTo($"-- Begin of chunk : {ChunkName} "));
                Assert.That(source.Lines[1], Is.EqualTo("local one = 1"));
                Assert.That(source.Lines[2], Is.EqualTo("return one"));
                Assert.That(source.OwnerScript, Is.SameAs(script));
                Assert.That(source.Name, Is.EqualTo(ChunkName));
                Assert.That(source.Code, Is.EqualTo(Code));
            });
        }

        [Test]
        public void GetCodeSnippetThrowsWhenSourceRefIsNull()
        {
            Script script = new();

            script.DoString("return 1", null, "units/source/null");
            SourceCode source = script.GetSourceCode(script.SourceCodeCount - 1);

            Assert.That(
                () => source.GetCodeSnippet(null),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("sourceCodeRef")
            );
        }

        [Test]
        public void GetCodeSnippetAppendsIntermediateLines()
        {
            Script script = new();
            script.DoString(
                "local one = 1\nlocal two = one + 1\nlocal three = two + 1\nreturn three",
                null,
                "units/source/multi"
            );
            SourceCode source = script.GetSourceCode(script.SourceCodeCount - 1);

            SourceRef span = new SourceRef(
                source.SourceId,
                from: 0,
                to: source.Lines[3].Length - 1,
                fromline: 1,
                toline: 3,
                isStepStop: false
            );

            string snippet = source.GetCodeSnippet(span);

            string expected = string.Concat(source.Lines[1], source.Lines[2], source.Lines[3]);

            Assert.That(snippet, Is.EqualTo(expected));
        }
    }
}
