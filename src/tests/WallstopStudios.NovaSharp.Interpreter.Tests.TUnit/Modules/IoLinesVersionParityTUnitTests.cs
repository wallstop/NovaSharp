namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Tests for <c>io.lines</c> version-specific behavior:
    /// <list type="bullet">
    /// <item>Lua 5.1-5.3: <c>io.lines(filename)</c> returns iterator triple (iterator, nil, nil)</item>
    /// <item>Lua 5.4+: <c>io.lines(filename)</c> returns quadruple (iterator, nil, nil, file_handle)</item>
    /// </list>
    /// </summary>
    public sealed class IoLinesVersionParityTUnitTests
    {
        // =============================================================================
        // io.lines return value count tests
        // =============================================================================

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task IoLinesReturnsThreeValuesInLua51To53(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            using TempFileScope temp = TempFileScope.CreateWithText("line1\nline2\nline3\n");
            string path = temp.EscapedPath;

            DynValue result = script.DoString(
                $@"
                local a, b, c, d = io.lines('{path}')
                -- a is callable (either function or userdata with __call)
                local isCallable = type(a) == 'function' or (type(a) == 'userdata' and pcall(function() return a() end))
                return isCallable, b, c, d
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            // First value should be callable
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[2].IsNil()).IsTrue().ConfigureAwait(false);
            // In 5.1-5.3, 4th return value should be nil
            await Assert.That(result.Tuple[3].IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task IoLinesReturnsFourValuesInLua54Plus(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            using TempFileScope temp = TempFileScope.CreateWithText("line1\nline2\nline3\n");
            string path = temp.EscapedPath;

            DynValue result = script.DoString(
                $@"
                local a, b, c, d = io.lines('{path}')
                -- a is callable (either function or userdata with __call)
                local isCallable = type(a) == 'function' or (type(a) == 'userdata' and pcall(function() return a() end))
                return isCallable, b, c, io.type(d)
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            // First value should be callable
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[2].IsNil()).IsTrue().ConfigureAwait(false);
            // In 5.4+, 4th return value should be a file handle
            await Assert.That(result.Tuple[3].String).IsEqualTo("file").ConfigureAwait(false);
        }

        // =============================================================================
        // io.lines iteration functionality tests (all versions)
        // =============================================================================

        [Test]
        [AllLuaVersions]
        public async Task IoLinesIteratesOverAllLines(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            using TempFileScope temp = TempFileScope.CreateWithText("first\nsecond\nthird\n");
            string path = temp.EscapedPath;

            DynValue result = script.DoString(
                $@"
                local lines = {{}}
                for line in io.lines('{path}') do
                    lines[#lines + 1] = line
                end
                return #lines, lines[1], lines[2], lines[3]
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("first").ConfigureAwait(false);
            await Assert.That(result.Tuple[2].String).IsEqualTo("second").ConfigureAwait(false);
            await Assert.That(result.Tuple[3].String).IsEqualTo("third").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task IoLinesReturnsEmptyTableForEmptyFile(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            DynValue result = script.DoString(
                $@"
                local count = 0
                for line in io.lines('{path}') do
                    count = count + 1
                end
                return count
                "
            );

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task IoLinesHandlesSingleLineWithoutNewline(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            using TempFileScope temp = TempFileScope.CreateWithText("only line");
            string path = temp.EscapedPath;

            DynValue result = script.DoString(
                $@"
                local lines = {{}}
                for line in io.lines('{path}') do
                    lines[#lines + 1] = line
                end
                return #lines, lines[1]
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("only line").ConfigureAwait(false);
        }

        // =============================================================================
        // io.lines file handle tests (Lua 5.4+ specific)
        // =============================================================================

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task IoLinesFileHandleCanBeClosedManuallyInLua54Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            using TempFileScope temp = TempFileScope.CreateWithText("line1\nline2\n");
            string path = temp.EscapedPath;

            DynValue result = script.DoString(
                $@"
                local iter, a, b, fh = io.lines('{path}')
                local typeBeforeClose = io.type(fh)
                fh:close()
                local typeAfterClose = io.type(fh)
                return typeBeforeClose, typeAfterClose
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].String).IsEqualTo("file").ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .IsEqualTo("closed file")
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task IoLinesFileHandleIsValidDuringIterationInLua54Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            using TempFileScope temp = TempFileScope.CreateWithText("line1\nline2\nline3\n");
            string path = temp.EscapedPath;

            DynValue result = script.DoString(
                $@"
                local iter, a, b, fh = io.lines('{path}')
                local typesDuringIteration = {{}}
                local lineCount = 0
                for line in iter, a, b do
                    lineCount = lineCount + 1
                    typesDuringIteration[lineCount] = io.type(fh)
                    if lineCount >= 2 then break end
                end
                return typesDuringIteration[1], typesDuringIteration[2], io.type(fh)
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            // File should remain open during partial iteration
            await Assert.That(result.Tuple[0].String).IsEqualTo("file").ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("file").ConfigureAwait(false);
            // File should still be open after breaking out of loop
            await Assert.That(result.Tuple[2].String).IsEqualTo("file").ConfigureAwait(false);
        }

        // =============================================================================
        // io.lines error handling tests (all versions)
        // =============================================================================

        [Test]
        [AllLuaVersions]
        public void IoLinesThrowsForNonexistentFile(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            using TempFileScope missingFile = TempFileScope.Create(extension: ".txt");
            string path = missingFile.EscapedPath;

            Assert.Throws<WallstopStudios.NovaSharp.Interpreter.Errors.ScriptRuntimeException>(() =>
            {
                script.DoString($"for line in io.lines('{path}') do end");
            });
        }
    }
}
