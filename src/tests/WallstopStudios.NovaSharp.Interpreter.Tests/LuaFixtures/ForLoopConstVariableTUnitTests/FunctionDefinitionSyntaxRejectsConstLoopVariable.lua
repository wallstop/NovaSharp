-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ForLoopConstVariableTUnitTests.cs:71
-- @test: ForLoopConstVariableTUnitTests.FunctionDefinitionSyntaxRejectsConstLoopVariable
-- Compatibility notes: Test targets Lua 5.4+
for i = 1, 3 do function i() end end
