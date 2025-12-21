-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:808
-- @test: StringModuleTUnitTests.GSubAppliesGlobalReplacement
-- @compat-notes: Test targets Lua 5.1
local replaced, count = string.gsub('foo bar foo', 'foo', 'baz')
                return replaced, count
