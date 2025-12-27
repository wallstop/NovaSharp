-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:337
-- @test: StringModuleTUnitTests.ByteReturnsNilForNaNIndexLua51And52
-- @compat-notes: Test targets Lua 5.1
return string.byte('Lua', 0/0)
