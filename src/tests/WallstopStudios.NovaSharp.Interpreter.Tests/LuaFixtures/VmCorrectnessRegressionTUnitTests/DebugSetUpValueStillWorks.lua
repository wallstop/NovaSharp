-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/VmCorrectnessRegressionTUnitTests.cs:137
-- @test: VmCorrectnessRegressionTUnitTests.DebugSetUpValueStillWorks
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local x = 10
                local function f()
                    return x
                end
                debug.setupvalue(f, {xIndex}, 99)
                return f()
