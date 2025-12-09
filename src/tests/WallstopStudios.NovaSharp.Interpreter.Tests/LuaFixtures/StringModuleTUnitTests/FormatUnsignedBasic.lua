-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:992
-- @test: StringModuleTUnitTests.FormatUnsignedBasic
return string.format('%u', 42)
