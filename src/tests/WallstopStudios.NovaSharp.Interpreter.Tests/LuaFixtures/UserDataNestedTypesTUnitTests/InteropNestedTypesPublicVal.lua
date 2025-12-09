-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataNestedTypesTUnitTests.cs:104
-- @test: UserDataNestedTypesTUnitTests.InteropNestedTypesPublicVal
return o.SomeNestedType:Get()
