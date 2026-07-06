-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:376
-- @test: TailCallTUnitTests.CallableTableTailCallReportsTailCallFrame
-- Compatibility notes: Test targets Lua 5.2+
local callable = setmetatable({}, {
                    __call = function()
                        return debug.getinfo(1, 't').istailcall
                    end
                })

                local function caller()
                    return callable()
                end

                return caller()
