-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataNestedTypesTUnitTests.cs:66
-- @test: UserDataNestedTypesTUnitTests.InteropNestedTypesPrivateRef
return o.SomeNestedTypePrivate:Get()
