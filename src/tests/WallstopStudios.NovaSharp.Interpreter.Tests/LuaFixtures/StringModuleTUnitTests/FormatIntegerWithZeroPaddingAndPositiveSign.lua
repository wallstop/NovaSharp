-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1162
-- @test: StringModuleTUnitTests.FormatIntegerWithZeroPaddingAndPositiveSign
return string.format('%+08d', 42)
