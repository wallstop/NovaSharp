-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxAccessRestrictionTUnitTests.cs:79
-- @test: SandboxAccessRestrictionTUnitTests.UnrestrictedFunctionExecutesNormally
return load('return 42')()
