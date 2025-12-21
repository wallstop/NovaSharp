-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1092
-- @test: StringModuleTUnitTests.GMatchWithInitAtExactWordBoundary
-- @compat-notes: Test targets Lua 5.4+
local results = {}
                -- 'hello world' - 'world' starts at position 7
                for m in string.gmatch('hello world', '%w+', 7) do
                    results[#results + 1] = m
                end
                return table.concat(results, ',')
