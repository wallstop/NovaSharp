-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Tree\Statements\AssignmentStatementTUnitTests.cs:100
-- @test: AssignmentStatementTUnitTests.AssignmentRequiresWritableVariables
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: bitwise operators
1 = 2
