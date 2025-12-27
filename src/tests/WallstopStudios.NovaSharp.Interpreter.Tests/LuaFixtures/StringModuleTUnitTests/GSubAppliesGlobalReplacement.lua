-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:560
-- @test: StringModuleTUnitTests.GSubAppliesGlobalReplacement
local replaced, count = string.gsub('foo bar foo', 'foo', 'baz')
                return replaced, count
