-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:675
-- @test: ErrorHandlingModuleTUnitTests.NestedXpcallTailHandledErrorDoesNotInvokeOuterHandler
local outer_count = 0
                local inner_count = 0

                local function fail()
                    error('err', 0)
                end

                local function middle()
                    return xpcall(function()
                        return fail()
                    end, function(message)
                        inner_count = inner_count + 1
                        return 'inner:' .. message
                    end)
                end

                local ok, protected_ok, message = xpcall(function()
                    return middle()
                end, function(message)
                    outer_count = outer_count + 1
                    return 'outer:' .. message
                end)

                return ok, protected_ok, message, outer_count, inner_count
