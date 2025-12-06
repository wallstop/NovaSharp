-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/NovaSharpHideMemberAttributeTUnitTests.cs:26
-- @test: NovaSharpHideMemberAttributeTUnitTests.HiddenMembersAreNotExposedToScripts
return sample.VisibleMethod()
