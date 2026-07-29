-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:218
-- @test: StringModuleTUnitTests.ByteSupportsNegativeIndices
return string.byte('Lua', -1)
