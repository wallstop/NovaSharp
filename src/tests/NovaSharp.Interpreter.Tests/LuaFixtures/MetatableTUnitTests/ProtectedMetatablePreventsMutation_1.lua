-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/MetatableTUnitTests.cs:128
-- @test: MetatableTUnitTests.ProtectedMetatablePreventsMutation
return getmetatable(subject)
