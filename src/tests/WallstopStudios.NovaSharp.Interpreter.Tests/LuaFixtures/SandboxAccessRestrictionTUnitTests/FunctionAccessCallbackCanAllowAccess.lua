-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxAccessRestrictionTUnitTests.cs:102
-- @test: SandboxAccessRestrictionTUnitTests.FunctionAccessCallbackCanAllowAccess
return load('return 99')()
