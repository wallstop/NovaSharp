-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataNestedTypesTUnitTests.cs:145
-- @test: UserDataNestedTypesTUnitTests.InteropNestedTypesPrivateVal2
return o.SomeNestedTypePrivate2:Get()
