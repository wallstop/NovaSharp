-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StructAssignmentTechniqueTUnitTests.cs:100
-- @test: Vector3Accessor.StructFieldCantSetThroughLua
-- @compat-notes: Lua 5.3+: bitwise operators
transform.Position.X = 15;
