-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxAccessRestrictionTUnitTests.cs:29
-- @test: SandboxAccessRestrictionTUnitTests.RestrictedFunctionLoadThrowsSandboxViolationException
-- @compat-notes: Test class 'SandboxAccessRestrictionTUnitTests' uses NovaSharp-specific Sandbox functionality
return load('return 42')()
