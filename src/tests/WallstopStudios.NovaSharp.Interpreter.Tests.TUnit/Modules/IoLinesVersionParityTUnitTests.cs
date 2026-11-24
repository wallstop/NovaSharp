namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::NovaSharp;
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
    /// <item>Lua 5.1-5.3: <c>io.lines(filename)</c> returns one iterator function</item>
    /// <item>Lua 5.4+: <c>io.lines(filename)</c> returns quadruple (iterator, nil, nil, file_handle)</item>
    /// </list>
    /// </summary>
    public sealed class IoLinesVersionParityTUnitTests
    {
        [Test]
        [AllLuaVersions]
        public void IoLinesReturnsOwnedIteratorAndRejectsCallsAfterEof(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local path = os.tmpname()
                local output = assert(io.open(path, 'w'))
                assert(output:write('first\n'))
                assert(output:close())

                local function check(...)
                    local iterator, state, control, owner = ...
                    local line = iterator()
                    local eofCount = select('#', iterator())
                    for i = 1, 2 do
                        local ok, problem = pcall(iterator)
                        assert(not ok and string.find(problem, 'file is already closed', 1, true),
                            'closed iterator must report a Lua error')
                    end

                    local hasCloseValue = _VERSION == 'Lua 5.4' or _VERSION == 'Lua 5.5'
                    assert(select('#', ...) == (hasCloseValue and 4 or 1), 'io.lines result count')
                    assert(type(iterator) == 'function' and state == nil and control == nil,
                        'io.lines iterator shape')
                    assert(io.type(owner) == (hasCloseValue and 'closed file' or nil), 'owned file')
                    assert(line == 'first' and eofCount == 0, 'line and EOF results')
                end

                local ok, problem = pcall(function() check(io.lines(path)) end)
                assert(os.remove(path))
                assert(ok, problem)
                "
            );
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

            LuaValue result = script.DoString(
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

            LuaValue result = script.DoString(
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

            LuaValue result = script.DoString(
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

            LuaValue result = script.DoString(
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

            LuaValue result = script.DoString(
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

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public void IoLinesClosesItsFileOnScopeExitAndAtEndOfInput(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            script.DoString(
                @"
                local path = os.tmpname()
                local input = assert(io.open(path, 'w'))
                assert(input:write('first\nsecond\n'))
                assert(input:close())

                local file
                local function check()
                    local iter, state, control
                    iter, state, control, file = io.lines(path)
                    for line in iter, state, control, file do
                        assert(line == 'first' and io.type(file) == 'file')
                        break
                    end
                    assert(io.type(file) == 'closed file', 'break did not close file')

                    iter, state, control, file = io.lines(path)
                    for line in iter, state, control, file do
                        assert(file:close())
                        break
                    end
                    assert(io.type(file) == 'closed file', 'manual close must survive scope exit')
                    local ok, problem = pcall(iter)
                    assert(not ok and string.find(problem, 'file is already closed', 1, true),
                        'manual close must invalidate iterator')

                    iter, state, control, file = io.lines(path)
                    assert(iter() == 'first')
                    assert(iter() == 'second')
                    assert(io.type(file) == 'file')
                    assert(iter() == nil)
                    assert(io.type(file) == 'closed file', 'iterator must close its file at EOF')
                end

                local ok, problem = pcall(check)
                if io.type(file) == 'file' then file:close() end
                assert(os.remove(path))
                assert(ok, problem)
                "
            );
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
