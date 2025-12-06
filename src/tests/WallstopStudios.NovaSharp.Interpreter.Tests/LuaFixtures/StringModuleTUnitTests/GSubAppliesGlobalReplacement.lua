-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:268
-- @test: StringModuleTUnitTests.GSubAppliesGlobalReplacement
-- @compat-notes: Lua 5.3+: bitwise operators
local replaced, count = string.gsub('foo bar foo', 'foo', 'baz')
                return replaced, count
