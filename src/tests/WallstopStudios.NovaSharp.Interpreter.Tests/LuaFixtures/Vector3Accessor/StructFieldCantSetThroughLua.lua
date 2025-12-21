-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StructAssignmentTechniqueTUnitTests.cs:103
-- @test: Vector3Accessor.StructFieldCantSetThroughLua
transform.Position.X = 15;
