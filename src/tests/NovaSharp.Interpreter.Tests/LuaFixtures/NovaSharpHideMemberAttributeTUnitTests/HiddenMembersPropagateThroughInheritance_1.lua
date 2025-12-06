-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/NovaSharpHideMemberAttributeTUnitTests.cs:50
-- @test: NovaSharpHideMemberAttributeTUnitTests.HiddenMembersPropagateThroughInheritance
return sample.HiddenProperty
