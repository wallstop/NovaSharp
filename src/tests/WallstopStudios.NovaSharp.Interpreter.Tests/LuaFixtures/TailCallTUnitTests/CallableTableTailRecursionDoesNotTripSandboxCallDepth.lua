-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:488
-- @test: TailCallTUnitTests.CallableTableTailRecursionDoesNotTripSandboxCallDepth
local callable
                callable = setmetatable({}, {
                    __call = function(_, n, acc)
                        if n == 0 then
                            return acc
                        end

                        return callable(n - 1, acc + 1)
                    end
                })

                function run_callable_tail_recursion()
                    return callable(100, 0)
                end
