-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:527
-- @test: StringModuleTUnitTests.GSubAppliesGlobalReplacement
-- @compat-notes: Lua 5.3+: bitwise operators
local replaced, count = string.gsub('foo bar foo', 'foo', 'baz')
                return replaced, count
