-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:151
-- @test: StringModuleTUnitTests.ByteSupportsNegativeIndices
return string.byte('Lua', -1)
