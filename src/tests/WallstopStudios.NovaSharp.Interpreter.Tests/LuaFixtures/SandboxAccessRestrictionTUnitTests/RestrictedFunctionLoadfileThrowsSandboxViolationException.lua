-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxAccessRestrictionTUnitTests.cs:41
-- @test: SandboxAccessRestrictionTUnitTests.RestrictedFunctionLoadfileThrowsSandboxViolationException
return loadfile('test.lua')
