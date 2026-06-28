namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Cli;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;

    /// <summary>
    /// Tests for <see cref="CliArgumentRegistry"/> and related CLI argument parsing infrastructure.
    /// </summary>
    public sealed class CliArgumentRegistryTUnitTests
    {
        // Static readonly arrays to satisfy CA1861
        private static readonly string[] SingleHelpArg = { "-H" };
        private static readonly string[] SingleHelpLongArg = { "--help" };
        private static readonly string[] SingleHelpAltArg = { "-?" };
        private static readonly string[] SingleHelpSlashArg = { "/?" };
        private static readonly string[] SingleExecuteShort = { "-e", "print('hello')" };
        private static readonly string[] MultipleExecuteArgs = { "-e", "x = 1", "-e", "print(x)" };
        private static readonly string[] ExecuteLongForm = { "--execute", "return 42" };
        private static readonly string[] ExecuteWithVersion =
        {
            "-v",
            "5.1",
            "-e",
            "print(_VERSION)",
        };
        private static readonly string[] ExecuteMissingValue = { "-e" };
        private static readonly string[] ScriptOnly = { "script.lua" };
        private static readonly string[] ScriptWithVersion = { "-v", "5.4", "myscript.lua" };
        private static readonly string[] ScriptWithLongVersion =
        {
            "--lua-version",
            "5.3",
            "test.lua",
        };
        private static readonly string[] ScriptVersionAfter = { "test.lua", "-v", "5.2" };
        private static readonly string[] CommandMode = { "-X", "help" };
        private static readonly string[] CommandMissingArg = { "-X" };
        private static readonly string[] HardwireBasic = { "-W", "dump.lua", "output.cs" };
        private static readonly string[] HardwireAllOptions =
        {
            "-W",
            "dump.lua",
            "output.vb",
            "--internals",
            "--vb",
            "--class:MyClass",
            "--namespace:MyNamespace",
        };
        private static readonly string[] HardwireMissingDest = { "-W", "dump.lua" };
        private static readonly string[] UnknownOption = { "--unknown-option" };
        private static readonly string[] InvalidVersion = { "-v", "invalid", "script.lua" };
        private static readonly string[] VersionMissingValue = { "-v" };
        private static readonly string[] ExecuteAndScript = { "-e", "print(1)", "script.lua" };

        #region TryParseLuaVersion Tests

        [global::TUnit.Core.Test]
        [Arguments("5.1", LuaCompatibilityVersion.Lua51)]
        [Arguments("5.2", LuaCompatibilityVersion.Lua52)]
        [Arguments("5.3", LuaCompatibilityVersion.Lua53)]
        [Arguments("5.4", LuaCompatibilityVersion.Lua54)]
        [Arguments("5.5", LuaCompatibilityVersion.Lua55)]
        [Arguments("51", LuaCompatibilityVersion.Lua51)]
        [Arguments("52", LuaCompatibilityVersion.Lua52)]
        [Arguments("53", LuaCompatibilityVersion.Lua53)]
        [Arguments("54", LuaCompatibilityVersion.Lua54)]
        [Arguments("55", LuaCompatibilityVersion.Lua55)]
        [Arguments("lua5.1", LuaCompatibilityVersion.Lua51)]
        [Arguments("lua5.4", LuaCompatibilityVersion.Lua54)]
        [Arguments("Lua5.4", LuaCompatibilityVersion.Lua54)]
        [Arguments("LUA5.4", LuaCompatibilityVersion.Lua54)]
        [Arguments("lua51", LuaCompatibilityVersion.Lua51)]
        [Arguments("latest", LuaCompatibilityVersion.Latest)]
        [Arguments("LATEST", LuaCompatibilityVersion.Latest)]
        [Arguments("Latest", LuaCompatibilityVersion.Latest)]
        public async Task TryParseLuaVersionParsesValidVersionStrings(
            string input,
            LuaCompatibilityVersion expected
        )
        {
            bool result = CliArgumentRegistry.TryParseLuaVersion(
                input,
                out LuaCompatibilityVersion version
            );

            await Assert.That(result).IsTrue().ConfigureAwait(false);
            await Assert.That(version).IsEqualTo(expected).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(null)]
        [Arguments("")]
        [Arguments("   ")]
        [Arguments("invalid")]
        [Arguments("5.0")]
        [Arguments("5.6")]
        [Arguments("50")]
        [Arguments("56")]
        [Arguments("abc")]
        [Arguments("5.1.1")]
        public async Task TryParseLuaVersionReturnsFalseForInvalidVersionStrings(string input)
        {
            bool result = CliArgumentRegistry.TryParseLuaVersion(
                input,
                out LuaCompatibilityVersion version
            );

            await Assert.That(result).IsFalse().ConfigureAwait(false);
            // When parsing fails, version should be set to Latest (default)
            await Assert
                .That(version)
                .IsEqualTo(LuaCompatibilityVersion.Latest)
                .ConfigureAwait(false);
        }

        #endregion

        #region Parse - Empty/REPL Mode Tests

        [global::TUnit.Core.Test]
        public async Task ParseWithNoArgumentsReturnsReplMode()
        {
            CliParseResult result = CliArgumentRegistry.Parse(Array.Empty<string>());

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Mode).IsEqualTo(CliExecutionMode.Repl).ConfigureAwait(false);
            await Assert.That(result.ShouldExit).IsFalse().ConfigureAwait(false);
            await Assert.That(result.LuaVersion.HasValue).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithNullArgumentsReturnsReplMode()
        {
            CliParseResult result = CliArgumentRegistry.Parse(null);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Mode).IsEqualTo(CliExecutionMode.Repl).ConfigureAwait(false);
        }

        #endregion

        #region Parse - Help Mode Tests

        [global::TUnit.Core.Test]
        public async Task ParseWithHelpShortReturnsHelpMode()
        {
            CliParseResult result = CliArgumentRegistry.Parse(SingleHelpArg);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Mode).IsEqualTo(CliExecutionMode.Help).ConfigureAwait(false);
            await Assert.That(result.ShouldExit).IsTrue().ConfigureAwait(false);
            await Assert.That(result.ExitCode).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithHelpLongReturnsHelpMode()
        {
            CliParseResult result = CliArgumentRegistry.Parse(SingleHelpLongArg);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Mode).IsEqualTo(CliExecutionMode.Help).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithHelpAltReturnsHelpMode()
        {
            CliParseResult result = CliArgumentRegistry.Parse(SingleHelpAltArg);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Mode).IsEqualTo(CliExecutionMode.Help).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithHelpSlashReturnsHelpMode()
        {
            CliParseResult result = CliArgumentRegistry.Parse(SingleHelpSlashArg);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Mode).IsEqualTo(CliExecutionMode.Help).ConfigureAwait(false);
        }

        #endregion

        #region Parse - Execute Mode Tests

        [global::TUnit.Core.Test]
        public async Task ParseWithSingleExecuteArgumentReturnsExecuteMode()
        {
            CliParseResult result = CliArgumentRegistry.Parse(SingleExecuteShort);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Mode)
                .IsEqualTo(CliExecutionMode.Execute)
                .ConfigureAwait(false);
            await Assert.That(result.InlineChunks.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(result.InlineChunks[0])
                .IsEqualTo("print('hello')")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithMultipleExecuteArgumentsReturnsAllChunks()
        {
            CliParseResult result = CliArgumentRegistry.Parse(MultipleExecuteArgs);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Mode)
                .IsEqualTo(CliExecutionMode.Execute)
                .ConfigureAwait(false);
            await Assert.That(result.InlineChunks.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.InlineChunks[0]).IsEqualTo("x = 1").ConfigureAwait(false);
            await Assert.That(result.InlineChunks[1]).IsEqualTo("print(x)").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithLongExecuteArgumentWorks()
        {
            CliParseResult result = CliArgumentRegistry.Parse(ExecuteLongForm);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Mode)
                .IsEqualTo(CliExecutionMode.Execute)
                .ConfigureAwait(false);
            await Assert.That(result.InlineChunks[0]).IsEqualTo("return 42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithExecuteAndVersionReturnsCorrectVersion()
        {
            CliParseResult result = CliArgumentRegistry.Parse(ExecuteWithVersion);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Mode)
                .IsEqualTo(CliExecutionMode.Execute)
                .ConfigureAwait(false);
            await Assert.That(result.LuaVersion.HasValue).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.LuaVersion.Value)
                .IsEqualTo(LuaCompatibilityVersion.Lua51)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithExecuteMissingValueReturnsError()
        {
            CliParseResult result = CliArgumentRegistry.Parse(ExecuteMissingValue);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.ErrorMessage).Contains("-e").ConfigureAwait(false);
        }

        #endregion

        #region Parse - Script Mode Tests

        [global::TUnit.Core.Test]
        public async Task ParseWithScriptPathReturnsScriptMode()
        {
            CliParseResult result = CliArgumentRegistry.Parse(ScriptOnly);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Mode).IsEqualTo(CliExecutionMode.Script).ConfigureAwait(false);
            await Assert.That(result.ScriptPath).IsEqualTo("script.lua").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithVersionAndScriptPathReturnsScriptModeWithVersion()
        {
            CliParseResult result = CliArgumentRegistry.Parse(ScriptWithVersion);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Mode).IsEqualTo(CliExecutionMode.Script).ConfigureAwait(false);
            await Assert.That(result.ScriptPath).IsEqualTo("myscript.lua").ConfigureAwait(false);
            await Assert
                .That(result.LuaVersion.Value)
                .IsEqualTo(LuaCompatibilityVersion.Lua54)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithLongVersionOptionWorks()
        {
            CliParseResult result = CliArgumentRegistry.Parse(ScriptWithLongVersion);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.LuaVersion.Value)
                .IsEqualTo(LuaCompatibilityVersion.Lua53)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithVersionAfterScriptPathWorks()
        {
            CliParseResult result = CliArgumentRegistry.Parse(ScriptVersionAfter);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.ScriptPath).IsEqualTo("test.lua").ConfigureAwait(false);
            await Assert
                .That(result.LuaVersion.Value)
                .IsEqualTo(LuaCompatibilityVersion.Lua52)
                .ConfigureAwait(false);
        }

        #endregion

        #region Parse - Command Mode Tests

        [global::TUnit.Core.Test]
        public async Task ParseWithCommandFlagReturnsCommandMode()
        {
            CliParseResult result = CliArgumentRegistry.Parse(CommandMode);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Mode)
                .IsEqualTo(CliExecutionMode.Command)
                .ConfigureAwait(false);
            await Assert.That(result.ReplCommand).IsEqualTo("help").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithCommandFlagMissingArgumentReturnsError()
        {
            CliParseResult result = CliArgumentRegistry.Parse(CommandMissingArg);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.ErrorMessage).Contains("-X").ConfigureAwait(false);
        }

        #endregion

        #region Parse - Hardwire Mode Tests

        [global::TUnit.Core.Test]
        public async Task ParseWithHardwireFlagReturnsHardwireMode()
        {
            CliParseResult result = CliArgumentRegistry.Parse(HardwireBasic);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Mode)
                .IsEqualTo(CliExecutionMode.Hardwire)
                .ConfigureAwait(false);
            await Assert
                .That(result.HardwireArgs.DumpFile)
                .IsEqualTo("dump.lua")
                .ConfigureAwait(false);
            await Assert
                .That(result.HardwireArgs.DestinationFile)
                .IsEqualTo("output.cs")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithHardwireAndAllOptionsWorks()
        {
            CliParseResult result = CliArgumentRegistry.Parse(HardwireAllOptions);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.HardwireArgs.AllowInternals).IsTrue().ConfigureAwait(false);
            await Assert.That(result.HardwireArgs.UseVisualBasic).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.HardwireArgs.ClassName)
                .IsEqualTo("MyClass")
                .ConfigureAwait(false);
            await Assert
                .That(result.HardwireArgs.NamespaceName)
                .IsEqualTo("MyNamespace")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithHardwireMissingFilesReturnsError()
        {
            CliParseResult result = CliArgumentRegistry.Parse(HardwireMissingDest);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.ErrorMessage)
                .Contains("dumpfile")
                .Or.Contains("destfile")
                .ConfigureAwait(false);
        }

        #endregion

        #region Parse - Error Handling Tests

        [global::TUnit.Core.Test]
        public async Task ParseWithUnknownOptionReturnsError()
        {
            CliParseResult result = CliArgumentRegistry.Parse(UnknownOption);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.ErrorMessage)
                .Contains("--unknown-option")
                .ConfigureAwait(false);
            await Assert.That(result.ExitCode).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithInvalidVersionReturnsError()
        {
            CliParseResult result = CliArgumentRegistry.Parse(InvalidVersion);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.ErrorMessage).Contains("invalid").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithVersionMissingValueReturnsError()
        {
            CliParseResult result = CliArgumentRegistry.Parse(VersionMissingValue);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.ErrorMessage).Contains("-v").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseWithExecuteAndScriptPathReturnsError()
        {
            CliParseResult result = CliArgumentRegistry.Parse(ExecuteAndScript);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.ErrorMessage)
                .Contains("-e")
                .Or.Contains("script")
                .ConfigureAwait(false);
        }

        #endregion

        #region GenerateHelpText Tests

        [global::TUnit.Core.Test]
        public async Task GenerateHelpTextContainsUsageSections()
        {
            string helpText = CliArgumentRegistry.GenerateHelpText();

            await Assert.That(helpText).Contains("USAGE:").ConfigureAwait(false);
            await Assert.That(helpText).Contains("HELP OPTIONS:").ConfigureAwait(false);
            await Assert.That(helpText).Contains("EXECUTION OPTIONS:").ConfigureAwait(false);
            await Assert.That(helpText).Contains("COMPATIBILITY OPTIONS:").ConfigureAwait(false);
            await Assert.That(helpText).Contains("TOOLING OPTIONS:").ConfigureAwait(false);
            await Assert.That(helpText).Contains("EXAMPLES:").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateHelpTextContainsAllArgumentDescriptions()
        {
            string helpText = CliArgumentRegistry.GenerateHelpText();

            // Check that key arguments are documented
            await Assert.That(helpText).Contains("-H").ConfigureAwait(false);
            await Assert.That(helpText).Contains("--help").ConfigureAwait(false);
            await Assert.That(helpText).Contains("-e").ConfigureAwait(false);
            await Assert.That(helpText).Contains("--execute").ConfigureAwait(false);
            await Assert.That(helpText).Contains("-v").ConfigureAwait(false);
            await Assert.That(helpText).Contains("--lua-version").ConfigureAwait(false);
            await Assert.That(helpText).Contains("-W").ConfigureAwait(false);
            await Assert.That(helpText).Contains("-X").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateHelpTextContainsValidLuaVersions()
        {
            string helpText = CliArgumentRegistry.GenerateHelpText();

            await Assert
                .That(helpText)
                .Contains(CliArgumentRegistry.ValidLuaVersions)
                .ConfigureAwait(false);
        }

        #endregion

        #region CliArgumentDefinition Tests

        [global::TUnit.Core.Test]
        public async Task ArgumentDefinitionMatchesShortForm()
        {
            bool matches = CliArgumentRegistry.Help.Matches("-H");

            await Assert.That(matches).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ArgumentDefinitionMatchesLongForm()
        {
            bool matches = CliArgumentRegistry.Help.Matches("--help");

            await Assert.That(matches).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ArgumentDefinitionDoesNotMatchDifferentArgument()
        {
            bool matches = CliArgumentRegistry.Help.Matches("-e");

            await Assert.That(matches).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ArgumentDefinitionDoesNotMatchNull()
        {
            bool matches = CliArgumentRegistry.Help.Matches(null);

            await Assert.That(matches).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ArgumentDefinitionFormatForUsageIncludesValuePlaceholder()
        {
            string formatted = CliArgumentRegistry.LuaVersion.FormatForUsage();

            await Assert.That(formatted).Contains("-v").ConfigureAwait(false);
            await Assert.That(formatted).Contains("--lua-version").ConfigureAwait(false);
            await Assert.That(formatted).Contains("<version>").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetAllDefinitionsReturnsAllRegisteredArguments()
        {
            IEnumerable<CliArgumentDefinition> definitions =
                CliArgumentRegistry.GetAllDefinitions();

            int count = 0;
            foreach (CliArgumentDefinition _ in definitions)
            {
                count++;
            }

            // Should have at least the main arguments
            await Assert.That(count).IsGreaterThanOrEqualTo(8).ConfigureAwait(false);
        }

        #endregion
    }
}
