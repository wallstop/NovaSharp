-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:570
-- @test: IoModuleTUnitTests.LinesWithoutArgumentsReadFromDefaultInput
-- @compat-notes: Test targets Lua 5.1
local results = {}
                for line in io.lines() do
                    table.insert(results, line)
                    if #results == 3 then break end
                end
                return results[1], results[2], results[3]
