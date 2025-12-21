-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxAccessRestrictionTUnitTests.cs:118
-- @test: SandboxAccessRestrictionTUnitTests.UnrestrictedFunctionExecutesNormallyLua51
-- @compat-notes: Test class 'SandboxAccessRestrictionTUnitTests' uses NovaSharp-specific Sandbox functionality
return loadstring('return 42')()
