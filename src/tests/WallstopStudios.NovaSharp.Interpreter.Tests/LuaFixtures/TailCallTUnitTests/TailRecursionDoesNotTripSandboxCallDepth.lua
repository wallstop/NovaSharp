-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:524
-- @test: TailCallTUnitTests.TailRecursionDoesNotTripSandboxCallDepth
-- Compatibility notes: Test targets Lua 5.4+
local function recur(n, acc)
                    if n == 0 then
                        return acc
                    end

                    return recur(n - 1, acc + 1)
                end

                return recur(100, 0)
