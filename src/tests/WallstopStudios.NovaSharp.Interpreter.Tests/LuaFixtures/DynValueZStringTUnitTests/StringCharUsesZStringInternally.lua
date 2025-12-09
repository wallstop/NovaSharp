-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/DynValueZStringTUnitTests.cs:247
-- @test: DynValueZStringTUnitTests.StringCharUsesZStringInternally
return string.char(72, 101, 108, 108, 111)
