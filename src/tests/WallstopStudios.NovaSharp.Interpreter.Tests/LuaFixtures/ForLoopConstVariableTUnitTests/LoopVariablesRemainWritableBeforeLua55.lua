-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ForLoopConstVariableTUnitTests.cs:116
-- @test: ForLoopConstVariableTUnitTests.LoopVariablesRemainWritableBeforeLua55
-- Compatibility notes: Test targets Lua 5.4+
for i = 1, 2 do i = i + 1 end for key, value in pairs({ 1 }) do key = 'x' value = 'y' end print('ok') return 'ok'
