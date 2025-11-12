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

        [Test]
        public void HexFloatLiteralParsesToExpectedNumber()
        {
            Script script = new();
            DynValue result = script.DoString("return 0x1.fp3");

            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(15.5d));
        }

        [Test]
        public void UnicodeEscapeSequenceIsDecoded()
        {
            Script script = new();
            DynValue result = script.DoString("return \"hi-\\u{1F40D}\"");

            Assert.That(result.Type, Is.EqualTo(DataType.String));
            Assert.That(result.String, Is.EqualTo("hi-\U0001F40D"));
        }

        [Test]
        public void MalformedHexLiteralThrowsSyntaxError()
        {
            Script script = new();

            Assert.That(
                () => script.DoString("return 0x1G"),
                Throws
                    .TypeOf<SyntaxErrorException>()
                    .With.Property(nameof(SyntaxErrorException.DecoratedMessage))
                    .Contains("near 'G'")
            );
        }

        [Test]
        public void DecimalEscapeTooLargeThrowsHelpfulMessage()
        {
            Script script = new();

            Assert.That(
                () => script.DoString("return \"\\400\""),
                Throws
                    .TypeOf<SyntaxErrorException>()
                    .With.Property(nameof(SyntaxErrorException.DecoratedMessage))
                    .Contains("decimal escape too large")
            );
        }
    }
}
