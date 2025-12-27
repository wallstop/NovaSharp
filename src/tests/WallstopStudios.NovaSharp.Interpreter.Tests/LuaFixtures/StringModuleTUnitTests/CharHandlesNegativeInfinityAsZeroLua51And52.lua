-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:949
-- @test: StringModuleTUnitTests.CharHandlesNegativeInfinityAsZeroLua51And52
-- @compat-notes: Test targets Lua 5.1
return string.char(-1/0)
