-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1029
-- @test: StringModuleTUnitTests.GMatchWithInitParameterStartsAtSpecifiedPosition
-- @compat-notes: Test targets Lua 5.1
local results = {}
                for m in string.gmatch('abc def ghi', '%w+', 5) do
                    results[#results + 1] = m
                end
                return table.concat(results, ',')
