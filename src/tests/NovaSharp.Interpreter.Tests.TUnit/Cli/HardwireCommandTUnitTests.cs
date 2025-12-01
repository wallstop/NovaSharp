#pragma warning disable CA2007

namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using NovaSharp.Interpreter.Tests.Units;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [PlatformDetectorIsolation]
    public sealed class HardwireCommandTUnitTests
    {
        private static readonly SemaphoreSlim DumpLoaderSemaphore = new(1, 1);

        [global::TUnit.Core.Test]
        public async Task ExecuteAbortOnQuitStopsInteractiveFlow()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            HardwireCommand command = new();
            string input = "#quit" + Environment.NewLine;
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleRedirectionScope consoleScope = new(input);
                command.Execute(new ShellContext(new Script()), string.Empty);
                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.HardwireCommandAbortHint);
            });
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteInvalidLuaFilePromptsForRetry()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            HardwireCommand command = new();
            string input =
                string.Join(Environment.NewLine, "cs", "nonexistent.lua", "#quit")
                + Environment.NewLine;

            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleRedirectionScope consoleScope = new(input);
                command.Execute(new ShellContext(new Script()), string.Empty);

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.HardwireMissingFile);
            });
        }

        [global::TUnit.Core.Test]
        public async Task GenerateWithMissingDumpFileReportsInternalError()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            string dumpPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");
            using TempFileScope destScope = TempFileScope.FromExisting(destPath);

            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleRedirectionScope consoleScope = new();
                HardwireCommand.Generate(
                    "cs",
                    dumpPath,
                    destPath,
                    allowInternals: false,
                    classname: "MissingTypes",
                    namespacename: "MissingNamespace"
                );

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.HardwireInternalError(string.Empty));
            });
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteInvalidNamespaceRequestsRetry()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope dumpScope = TempFileScope.Create(
                namePrefix: "dump_",
                extension: ".lua"
            );
            string dumpPath = dumpScope.FilePath;
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.cs");
            using TempFileScope destScope = TempFileScope.FromExisting(destPath);
            await File.WriteAllTextAsync(dumpPath, "return {}").ConfigureAwait(false);
            string input =
                string.Join(
                    Environment.NewLine,
                    "cs",
                    dumpPath,
                    destPath,
                    "y",
                    "123Invalid",
                    "#quit"
                ) + Environment.NewLine;

            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleRedirectionScope consoleScope = new(input);
                HardwireCommand command = new();
                command.Execute(new ShellContext(new Script()), string.Empty);

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.HardwireIdentifierValidation);
            });
        }

        [global::TUnit.Core.Test]
        public async Task GenerateCreatesCSharpSourceFromDump()
        {
            await GenerateAndVerify(
                    language: "cs",
                    allowInternals: false,
                    visibility: "public",
                    expectedNamespaceSnippet: "namespace GeneratedNamespace",
                    expectedClassSnippet: "class GeneratedTypes",
                    expectInternalWarning: false
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateWithInternalVisibilityAndNoInternalsEmitsWarning()
        {
            await GenerateAndVerify(
                    language: "cs",
                    allowInternals: false,
                    visibility: "internal",
                    expectedNamespaceSnippet: "namespace GeneratedNamespace",
                    expectedClassSnippet: "class GeneratedTypes",
                    expectInternalWarning: true
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateCreatesVbSourceWhenRequested()
        {
            await GenerateAndVerify(
                    language: "vb",
                    allowInternals: true,
                    visibility: "public",
                    expectedNamespaceSnippet: "Namespace GeneratedNamespace",
                    expectedClassSnippet: "Class GeneratedTypes",
                    expectInternalWarning: false
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateWithInternalsAllowedSuppressesWarning()
        {
            await GenerateAndVerify(
                    language: "cs",
                    allowInternals: true,
                    visibility: "internal",
                    expectedNamespaceSnippet: "namespace GeneratedNamespace",
                    expectedClassSnippet: "class GeneratedTypes",
                    expectInternalWarning: false
                )
                .ConfigureAwait(false);
        }

        private static async Task GenerateAndVerify(
            string language,
            bool allowInternals,
            string visibility,
            string expectedNamespaceSnippet,
            string expectedClassSnippet,
            bool expectInternalWarning
        )
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            string destPath = Path.Combine(
                Path.GetTempPath(),
                $"hardwire_{Guid.NewGuid():N}.{(language == "vb" ? "vb" : "cs")}"
            );
            using TempFileScope destScope = TempFileScope.FromExisting(destPath);
            await DumpLoaderSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using HardwireDumpLoaderScope dumpLoaderScope = HardwireDumpLoaderScope.Override(
                    _ =>
                    {
                        Script script = new(default(CoreModules));
                        return HardwireTestUtilities.CreateDescriptorTable(script, visibility);
                    }
                );

                string output = string.Empty;
                await ConsoleCaptureCoordinator
                    .RunAsync(() =>
                    {
                        using ConsoleRedirectionScope consoleScope = new();
                        HardwireCommand.Generate(
                            language,
                            "ignored.lua",
                            destPath,
                            allowInternals,
                            "GeneratedTypes",
                            "GeneratedNamespace"
                        );

                        output = consoleScope.Writer.ToString();
                        return Task.CompletedTask;
                    })
                    .ConfigureAwait(false);

                await Assert.That(File.Exists(destPath)).IsTrue();
                string generated = await File.ReadAllTextAsync(destPath).ConfigureAwait(false);
                await Assert.That(generated).Contains(expectedNamespaceSnippet);
                await Assert.That(generated).Contains(expectedClassSnippet);
                await Assert
                    .That(output)
                    .Contains(
                        CliMessages.HardwireGenerationSummary(0, expectInternalWarning ? 1 : 0)
                    );
                if (expectInternalWarning)
                {
                    await Assert.That(output).Contains("visibility is 'internal'");
                }
                else
                {
                    await Assert.That(output).DoesNotContain("visibility is 'internal'");
                }
            }
            finally
            {
                DumpLoaderSemaphore.Release();
            }
        }
    }
}

#pragma warning restore CA2007
