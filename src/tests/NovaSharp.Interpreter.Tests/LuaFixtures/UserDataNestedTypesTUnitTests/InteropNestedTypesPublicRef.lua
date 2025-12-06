-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataNestedTypesTUnitTests.cs:46
-- @test: UserDataNestedTypesTUnitTests.InteropNestedTypesPublicRef
return o.SomeNestedType:Get()
