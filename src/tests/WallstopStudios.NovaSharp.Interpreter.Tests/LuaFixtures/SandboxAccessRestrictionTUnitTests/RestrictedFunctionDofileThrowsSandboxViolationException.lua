-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxAccessRestrictionTUnitTests.cs:77
-- @test: SandboxAccessRestrictionTUnitTests.RestrictedFunctionDofileThrowsSandboxViolationException
-- @compat-notes: Test class 'SandboxAccessRestrictionTUnitTests' uses NovaSharp-specific Sandbox functionality
dofile('test.lua')
