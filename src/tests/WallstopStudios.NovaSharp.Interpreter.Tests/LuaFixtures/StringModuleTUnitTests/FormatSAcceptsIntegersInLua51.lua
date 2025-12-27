-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1913
-- @test: StringModuleTUnitTests.FormatSAcceptsIntegersInLua51
-- @compat-notes: Test targets Lua 5.1; Uses injected variable: s
return string.format('%s', 123)
