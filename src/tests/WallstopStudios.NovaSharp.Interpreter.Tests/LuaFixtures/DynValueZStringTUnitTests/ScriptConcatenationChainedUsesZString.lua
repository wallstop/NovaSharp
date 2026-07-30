-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/DynValueZStringTUnitTests.cs:181
-- @test: DynValueZStringTUnitTests.ScriptConcatenationChainedUsesZString
return 'a' .. 'b' .. 'c' .. 'd' .. 'e'
