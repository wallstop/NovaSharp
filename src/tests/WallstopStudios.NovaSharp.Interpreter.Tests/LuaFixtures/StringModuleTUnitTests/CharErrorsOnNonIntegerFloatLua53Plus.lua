-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1020
-- @test: StringModuleTUnitTests.CharErrorsOnNonIntegerFloatLua53Plus
-- Test targets Lua 5.3+
return string.char(65.5)
