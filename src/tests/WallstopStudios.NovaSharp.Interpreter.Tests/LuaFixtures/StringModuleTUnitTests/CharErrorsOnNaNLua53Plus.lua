-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:972
-- @test: StringModuleTUnitTests.CharErrorsOnNaNLua53Plus
-- @compat-notes: Test targets Lua 5.1
return string.char(0/0)
