-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs:115
-- @test: IoLinesVersionParityTUnitTests.IoLinesReturnsEmptyTableForEmptyFile
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local count = 0
                for line in io.lines('{path}') do
                    count = count + 1
                end
                return count
