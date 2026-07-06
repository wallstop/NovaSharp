-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:426
-- @test: TailCallTUnitTests.XpcallTargetTailCallReportsTailCallFrame
-- Compatibility notes: Test targets Lua 5.2+
local function target()
                    return debug.getinfo(1, 't').istailcall
                end

                local ok, is_tail = xpcall(function()
                    return target()
                end, function(message)
                    return message
                end)

                return ok, is_tail
