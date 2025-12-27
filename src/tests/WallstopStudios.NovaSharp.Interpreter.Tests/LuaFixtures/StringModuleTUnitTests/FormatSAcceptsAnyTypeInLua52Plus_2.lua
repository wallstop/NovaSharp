-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2003
-- @test: StringModuleTUnitTests.FormatSAcceptsAnyTypeInLua52Plus
-- @compat-notes: Test targets Lua 5.2+; Uses injected variable: s
return string.format('%s', nil)
