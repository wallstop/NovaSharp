-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:929
-- @test: ScriptCallTUnitTests.LuaCallToLegacyClrFunctionPreservesTrailingTupleExpansion
-- Compatibility notes: Uses injected variable: callback
local function values()
                end

                return callback(10, values())
