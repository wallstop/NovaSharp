namespace NovaSharp.Interpreter.Tests.Units
{
    using Loaders;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public class ParserTests
    {
        [Test]
        public void SyntaxErrorsIncludeLineInformation()
        {
            Script script = new();
            Assert.That(
                () =>
                {
                    script.DoString(
                        @"
                        function broken()
                            local x =
                        end
                    "
                    );
                },
                Throws
                    .TypeOf<SyntaxErrorException>()
                    .With.Property(nameof(SyntaxErrorException.DecoratedMessage))
                    .Not.Null.And.Property(nameof(SyntaxErrorException.DecoratedMessage))
                    .Contains("chunk_1")
                    .And.Property(nameof(SyntaxErrorException.DecoratedMessage))
                    .Matches(@".*\(\d+,\d+-\d+\).*")
            );
        }

        [Test]
        public void LoadStringReportsFriendlyChunkName()
        {
            Script script = new();
            ScriptLoaderBase loader = (ScriptLoaderBase)script.Options.ScriptLoader;
            loader.IgnoreLuaPathGlobal = true;

            Assert.That(
                () => script.LoadString("local = 1", null, "tests/parser/chunk"),
                Throws
                    .TypeOf<SyntaxErrorException>()
                    .With.Property(nameof(SyntaxErrorException.DecoratedMessage))
                    .Contains("tests/parser/chunk")
            );
        }
    }
}
