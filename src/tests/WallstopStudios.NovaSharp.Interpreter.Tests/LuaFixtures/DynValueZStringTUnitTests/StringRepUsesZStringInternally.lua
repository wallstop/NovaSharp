-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/DynValueZStringTUnitTests.cs:229
-- @test: DynValueZStringTUnitTests.StringRepUsesZStringInternally
return string.rep('ab', 5)
