-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:228
-- @test: StringModuleTUnitTests.ByteReturnsNilWhenIndexPastEnd
return string.byte('Lua', 4)
