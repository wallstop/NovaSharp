-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs:150
-- @test: IoLinesVersionParityTUnitTests.IoLinesHandlesSingleLineWithoutNewline
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local lines = {{}}
                for line in io.lines('{path}') do
                    lines[#lines + 1] = line
                end
                return #lines, lines[1]
