-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:286
-- @test: StringLibTUnitTests.StringGSubTreatsUnknownPercentEscapesAsLiteralInLua51
-- @compat-notes: Test targets Lua 5.1
local result, count = string.gsub('hello world', '%w+', '%e')
                return result, count
