-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ForLoopConstVariableTUnitTests.cs:68
-- @test: ForLoopConstVariableTUnitTests.GenericForValueVariableRemainsWritable
-- Compatibility notes: Test targets Lua 5.4+
for key, value in pairs({ 1 }) do value = 'changed' end return 'ok'
