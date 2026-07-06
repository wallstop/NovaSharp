-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:632
-- @test: ErrorHandlingModuleTUnitTests.NestedXpcallHandledErrorDoesNotInvokeOuterHandler
local outer_count = 0
                local inner_count = 0

                local function middle()
                    local ok, message = xpcall(function()
                        error('err', 0)
                    end, function(message)
                        inner_count = inner_count + 1
                        return 'inner:' .. message
                    end)

                    return ok, message
                end

                local ok, protected_ok, message = xpcall(function()
                    local inner_ok, inner_message = middle()
                    return inner_ok, inner_message
                end, function(message)
                    outer_count = outer_count + 1
                    return 'outer:' .. message
                end)

                return ok, protected_ok, message, outer_count, inner_count
