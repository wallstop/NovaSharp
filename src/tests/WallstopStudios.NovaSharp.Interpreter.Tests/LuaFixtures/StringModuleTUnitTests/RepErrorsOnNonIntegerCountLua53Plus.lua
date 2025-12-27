-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:645
-- @test: StringModuleTUnitTests.RepErrorsOnNonIntegerCountLua53Plus
-- @compat-notes: Test targets Lua 5.1
return string.rep('a', 2.5)
