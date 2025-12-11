-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\NovaSharpHideMemberAttributeTUnitTests.cs:30
-- @test: NovaSharpHideMemberAttributeTUnitTests.HiddenMembersAreNotExposedToScripts
return sample.HiddenMethod()
