-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:788
-- @test: StringModuleTUnitTests.GMatchWithNegativeInitStartsFromEnd
-- @compat-notes: Test targets Lua 5.4+
local results = {}
                -- 'abc def ghi' has length 11, init=-3 means start at position 9 (the 'g' in 'ghi')
                for m in string.gmatch('abc def ghi', '%w+', -3) do
                    results[#results + 1] = m
                end
                return table.concat(results, ',')
