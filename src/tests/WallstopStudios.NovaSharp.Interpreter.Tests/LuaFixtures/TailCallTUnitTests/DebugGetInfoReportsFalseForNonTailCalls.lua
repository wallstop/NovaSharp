-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:265
-- @test: TailCallTUnitTests.DebugGetInfoReportsFalseForNonTailCalls
-- Compatibility notes: Test targets Lua 5.2+
local function target()
                    return debug.getinfo(1, 't').istailcall
                end

                local function caller()
                    local is_tail = target()
                    return is_tail
                end

                return caller()
