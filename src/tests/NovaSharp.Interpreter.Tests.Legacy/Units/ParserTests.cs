using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Loaders;
using NUnit.Framework;

namespace NovaSharp.Interpreter.Tests.Units
{
    [TestFixture]
    public class ParserTests
    {
        [Test]
        public void SyntaxErrorsIncludeLineInformation()
        {
            var script = new Script();
            var exception = Assert.Throws<SyntaxErrorException>(() =>
            {
                script.DoString(
                    @"
                    function broken()
                        local x =
                    end
                "
                );
            });

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.DecoratedMessage, Is.Not.Null);
            Assert.That(exception.DecoratedMessage, Does.Contain("chunk_1"));
            Assert.That(exception.DecoratedMessage, Does.Match(@".*\(\d+,\d+-\d+\).*"));
        }

        [Test]
        public void LoadStringReportsFriendlyChunkName()
        {
            var script = new Script();
            var loader = (ScriptLoaderBase)script.Options.ScriptLoader;
            loader.IgnoreLuaPathGlobal = true;

            var exception = Assert.Throws<SyntaxErrorException>(() =>
            {
                script.LoadString("local = 1", null, "tests/parser/chunk");
            });

            Assert.That(exception.DecoratedMessage, Does.Contain("tests/parser/chunk"));
        }
    }
}
