-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:160
-- @test: StringModuleTUnitTests.ByteReturnsNilWhenIndexPastEnd
return string.byte('Lua', 4)
