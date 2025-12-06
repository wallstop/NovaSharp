-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/MetatableTUnitTests.cs:131
-- @test: MetatableTUnitTests.ProtectedMetatablePreventsMutation
return pcall(function() setmetatable(subject, {}) end)
