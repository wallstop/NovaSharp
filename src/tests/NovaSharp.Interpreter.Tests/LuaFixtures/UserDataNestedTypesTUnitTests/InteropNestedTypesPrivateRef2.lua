-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataNestedTypesTUnitTests.cs:87
-- @test: UserDataNestedTypesTUnitTests.InteropNestedTypesPrivateRef2
return o.SomeNestedTypePrivate2:Get()
