-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ForLoopConstVariableTUnitTests.cs:86
-- @test: ForLoopConstVariableTUnitTests.FunctionDefinitionSyntaxRejectsConstLocal
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: const attribute
local value <const> = 1 function value() end
