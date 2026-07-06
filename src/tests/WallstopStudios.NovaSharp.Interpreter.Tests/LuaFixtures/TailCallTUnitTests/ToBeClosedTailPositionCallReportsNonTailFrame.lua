-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:588
-- @test: TailCallTUnitTests.ToBeClosedTailPositionCallReportsNonTailFrame
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local mt = {
                    __close = function()
                    end
                }

                local function target()
                    return debug.getinfo(1, 't').istailcall
                end

                local function caller()
                    local handle <close> = setmetatable({}, mt)
                    return target()
                end

                return caller()
