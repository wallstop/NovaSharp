-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:439
-- @test: IoModuleTUnitTests.LinesWithoutArgumentsReadFromDefaultInput
-- @compat-notes: Lua 5.3+: bitwise operators
local results = {}
                for line in io.lines() do
                    table.insert(results, line)
                    if #results == 3 then break end
                end
                return results[1], results[2], results[3]
