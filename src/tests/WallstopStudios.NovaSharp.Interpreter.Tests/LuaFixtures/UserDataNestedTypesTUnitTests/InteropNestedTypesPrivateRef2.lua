-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataNestedTypesTUnitTests.cs:93
-- @test: UserDataNestedTypesTUnitTests.InteropNestedTypesPrivateRef2
return o.SomeNestedTypePrivate2:Get()
