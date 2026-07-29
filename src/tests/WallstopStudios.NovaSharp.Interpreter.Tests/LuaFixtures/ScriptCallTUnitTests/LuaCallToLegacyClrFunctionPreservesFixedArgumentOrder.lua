-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:896
-- @test: ScriptCallTUnitTests.LuaCallToLegacyClrFunctionPreservesFixedArgumentOrder
-- Compatibility notes: Uses injected variable: callback
return callback(1, 2, 3, 4, 5, 6, 7)
