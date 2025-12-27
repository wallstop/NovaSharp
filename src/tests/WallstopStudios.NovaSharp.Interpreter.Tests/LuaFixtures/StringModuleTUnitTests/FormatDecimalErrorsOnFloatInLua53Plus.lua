-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1605
-- @test: StringModuleTUnitTests.FormatDecimalErrorsOnFloatInLua53Plus
-- @compat-notes: Test targets Lua 5.1
return string.format('%d', 123.456)
