-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:619
-- @test: TailCallTUnitTests.NonCallableTailPositionWithCloseRunsCloseHandlerOnError
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local closed_with_error = false
                local mt = {
                    __close = function(_, err)
                        closed_with_error = err ~= nil
                    end
                }

                local function caller()
                    local handle <close> = setmetatable({}, mt)
                    local not_callable = {}
                    return not_callable()
                end

                local ok = pcall(caller)
                return ok, closed_with_error
