-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:401
-- @test: TailCallTUnitTests.PcallTargetTailCallReportsTailCallFrame
-- Compatibility notes: Test targets Lua 5.2+
local function target()
                    return debug.getinfo(1, 't').istailcall
                end

                local ok, is_tail = pcall(function()
                    return target()
                end)

                return ok, is_tail
