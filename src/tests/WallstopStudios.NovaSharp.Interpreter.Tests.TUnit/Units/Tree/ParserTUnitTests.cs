namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;

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

            await Assert.That(exception.DecoratedMessage).Contains("chunk_1").ConfigureAwait(false);
            await Assert
                .That(exception.DecoratedMessage)
                .Matches(@".*\(\d+,\d+-\d+\).*")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadStringReportsFriendlyChunkName()
        {
            Script script = new();

            // Optionally set IgnoreLuaPathGlobal if the loader supports it
            // This is not required for the test but can help avoid path resolution issues
            if (script.Options.ScriptLoader is ScriptLoaderBase loader)
            {
                loader.IgnoreLuaPathGlobal = true;
            }

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.LoadString("local = 1", null, "tests/parser/chunk")
            )!;

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("tests/parser/chunk")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HexFloatLiteralParsesToExpectedNumber()
        {
            Script script = new();
            DynValue result = script.DoString("return 0x1.fp3");

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(15.5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnicodeEscapeSequenceIsDecoded()
        {
            Script script = new();
            DynValue result = script.DoString("return \"hi-\\u{1F40D}\"");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("hi-\U0001F40D").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MalformedHexLiteralThrowsSyntaxError()
        {
            Script script = new();
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("return 0x1G")
            )!;

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("near 'G'")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DecimalEscapeTooLargeThrowsHelpfulMessage()
        {
            Script script = new();
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("return \"\\400\"")
            )!;

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("decimal escape too large")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for chunk names appearing in error messages.
        /// Verifies that user-provided chunk names are preserved in syntax error messages.
        /// </summary>
        [global::TUnit.Core.Test]
        [Arguments("local = 1", "my_chunk", "my_chunk")]
        [Arguments("local = 1", "tests/parser/chunk", "tests/parser/chunk")]
        [Arguments("local = 1", "file.lua", "file.lua")]
        [Arguments("local = 1", "nested/path/script.lua", "nested/path/script.lua")]
        public async Task LoadStringReportsFriendlyChunkNameDataDriven(
            string invalidCode,
            string chunkName,
            string expectedInMessage
        )
        {
            Script script = new();

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.LoadString(invalidCode, null, chunkName)
            )!;

            await Assert
                .That(exception.DecoratedMessage)
                .Contains(expectedInMessage)
                .Because(
                    $"Chunk name '{chunkName}' should appear in error message: {exception.DecoratedMessage}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that when no chunk name is provided, a default one is used.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task LoadStringUsesDefaultChunkNameWhenNotProvided()
        {
            Script script = new();

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.LoadString("local = 1")
            )!;

            // Default chunk name should be something like "chunk_0" or similar
            await Assert
                .That(exception.DecoratedMessage)
                .Matches(@".*chunk.*")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that the script loader type doesn't affect error message generation.
        /// This test is designed to work regardless of what type of loader is configured.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task SyntaxErrorsWorkWithAnyScriptLoaderType()
        {
            Script script = new();

            // Capture loader type for diagnostic purposes
            string loaderTypeName = script.Options.ScriptLoader?.GetType().Name ?? "null";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.LoadString("local = 1", null, "test_chunk")
            )!;

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("test_chunk")
                .Because(
                    $"Error message should contain chunk name regardless of loader type ({loaderTypeName})"
                )
                .ConfigureAwait(false);
        }
    }
}
