-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1287
-- @test: StringModuleTUnitTests.CharErrorsOnPositiveInfinityLua53Plus
-- @compat-notes: Test targets Lua 5.3+
return string.char(1/0)
