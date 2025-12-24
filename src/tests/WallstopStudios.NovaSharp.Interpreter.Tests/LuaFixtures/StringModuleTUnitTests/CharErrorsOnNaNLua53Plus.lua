-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1269
-- @test: StringModuleTUnitTests.CharErrorsOnNaNLua53Plus
-- @compat-notes: Test targets Lua 5.1
return string.char(0/0)
