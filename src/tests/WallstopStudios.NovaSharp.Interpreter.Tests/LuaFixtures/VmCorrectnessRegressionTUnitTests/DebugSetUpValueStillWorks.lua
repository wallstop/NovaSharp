-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/VmCorrectnessRegressionTUnitTests.cs:137
-- @test: VmCorrectnessRegressionTUnitTests.DebugSetUpValueStillWorks
-- Lua 5.2+: _ENV variable
local x = 10
                local function f()
                    return x
                end
                debug.setupvalue(f, {xIndex}, 99)
                return f()
