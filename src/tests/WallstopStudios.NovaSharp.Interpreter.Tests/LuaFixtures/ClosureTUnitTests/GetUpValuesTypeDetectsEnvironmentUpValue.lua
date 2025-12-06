-- @lua-versions: 5.2, 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ClosureTUnitTests.cs:48
-- @test: ClosureTUnitTests.GetUpValuesTypeDetectsEnvironmentUpValue
-- @compat-notes: Lua 5.2+: _ENV variable
return function() return _ENV end
