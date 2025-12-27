-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:238
-- @test: StringModuleTUnitTests.ByteReturnsNilWhenStartExceedsEnd
return string.byte('Lua', 3, 2)
