#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Loaders;

    public sealed class ParserTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task SyntaxErrorsIncludeLineInformation()
        {
            Script script = new();

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString(
                    @"
                    function broken()
                        local x =
                    end
                "
                )
            )!;

            await Assert.That(exception.DecoratedMessage).Contains("chunk_1");
            await Assert.That(exception.DecoratedMessage).Matches(@".*\(\d+,\d+-\d+\).*");
        }

        [global::TUnit.Core.Test]
        public async Task LoadStringReportsFriendlyChunkName()
        {
            Script script = new();
            ScriptLoaderBase loader = (ScriptLoaderBase)script.Options.ScriptLoader;
            loader.IgnoreLuaPathGlobal = true;

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.LoadString("local = 1", null, "tests/parser/chunk")
            )!;

            await Assert.That(exception.DecoratedMessage).Contains("tests/parser/chunk");
        }

        [global::TUnit.Core.Test]
        public async Task HexFloatLiteralParsesToExpectedNumber()
        {
            Script script = new();
            DynValue result = script.DoString("return 0x1.fp3");

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(15.5d);
        }

        [global::TUnit.Core.Test]
        public async Task UnicodeEscapeSequenceIsDecoded()
        {
            Script script = new();
            DynValue result = script.DoString("return \"hi-\\u{1F40D}\"");

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("hi-\U0001F40D");
        }

        [global::TUnit.Core.Test]
        public async Task MalformedHexLiteralThrowsSyntaxError()
        {
            Script script = new();
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("return 0x1G")
            )!;

            await Assert.That(exception.DecoratedMessage).Contains("near 'G'");
        }

        [global::TUnit.Core.Test]
        public async Task DecimalEscapeTooLargeThrowsHelpfulMessage()
        {
            Script script = new();
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("return \"\\400\"")
            )!;

            await Assert.That(exception.DecoratedMessage).Contains("decimal escape too large");
        }
    }
}
#pragma warning restore CA2007
