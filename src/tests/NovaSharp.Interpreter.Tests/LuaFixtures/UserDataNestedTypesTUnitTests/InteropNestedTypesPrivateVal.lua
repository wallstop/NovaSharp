-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataNestedTypesTUnitTests.cs:124
-- @test: UserDataNestedTypesTUnitTests.InteropNestedTypesPrivateVal
return o.SomeNestedTypePrivate:Get()
