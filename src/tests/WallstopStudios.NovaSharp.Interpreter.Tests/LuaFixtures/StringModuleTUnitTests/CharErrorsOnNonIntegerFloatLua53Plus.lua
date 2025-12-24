-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1323
-- @test: StringModuleTUnitTests.CharErrorsOnNonIntegerFloatLua53Plus
-- @compat-notes: Test targets Lua 5.1
return string.char(65.5)
