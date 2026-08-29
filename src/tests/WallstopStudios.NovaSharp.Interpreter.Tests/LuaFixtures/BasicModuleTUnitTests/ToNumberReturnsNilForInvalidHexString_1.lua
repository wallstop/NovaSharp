-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:624
-- @test: BasicModuleTUnitTests.ToNumberReturnsNilForInvalidHexString
return tonumber('9223372036854775807') + 1
