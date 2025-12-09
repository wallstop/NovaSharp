-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxAccessRestrictionTUnitTests.cs:22
-- @test: SandboxAccessRestrictionTUnitTests.RestrictedFunctionLoadThrowsSandboxViolationException
return load('return 42')()
