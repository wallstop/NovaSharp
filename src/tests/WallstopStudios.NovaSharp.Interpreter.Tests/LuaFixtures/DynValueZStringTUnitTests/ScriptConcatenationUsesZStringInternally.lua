-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/DynValueZStringTUnitTests.cs:157
-- @test: DynValueZStringTUnitTests.ScriptConcatenationUsesZStringInternally
return 'hello' .. ' ' .. 'world'
