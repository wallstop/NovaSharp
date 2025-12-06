-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/TailCallTUnitTests.cs:15
-- @test: TailCallTUnitTests.TailRecursionHandlesThousandsOfFrames
-- @compat-notes: Lua 5.3+: bitwise operators
local function accumulate(n, acc)
                    if n == 0 then
                        return acc
                    end
                    return accumulate(n - 1, acc + 1)
                end

                return accumulate(20000, 0)
