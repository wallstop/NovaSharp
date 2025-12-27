-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1142
-- @test: StringModuleTUnitTests.FormatUnsignedWithFieldWidth
return string.format('%8u', 42)
