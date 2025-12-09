-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/MetatableTUnitTests.cs:131
-- @test: MetatableTUnitTests.ProtectedMetatablePreventsMutation
return pcall(function() setmetatable(subject, {}) end)
