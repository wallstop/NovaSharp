-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1115
-- @test: StringModuleTUnitTests.GMatchWithInitBeyondStringLengthReturnsNoMatches
-- @compat-notes: Test targets Lua 5.1
local count = 0
                for m in string.gmatch('abc', '%w+', 100) do
                    count = count + 1
                end
                return count
