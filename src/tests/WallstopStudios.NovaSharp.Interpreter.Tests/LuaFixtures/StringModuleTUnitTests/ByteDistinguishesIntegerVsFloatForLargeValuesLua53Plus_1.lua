-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:678
-- @test: StringModuleTUnitTests.ByteDistinguishesIntegerVsFloatForLargeValuesLua53Plus
-- @compat-notes: Test targets Lua 5.1
local x = 9007199254740993.0; return string.byte('a', x)
