-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/VmStackCeilingTUnitTests.cs:173
-- @test: VmStackCeilingTUnitTests.RepeatedCaughtStackOverflowsKeepVmHealthy
local function f(n) return 1 + f(n + 1) end
                local failures = 0
                for _ = 1, 50 do
                    local ok, err = pcall(f, 0)
                    if not ok and tostring(err):find('stack overflow') then
                        failures = failures + 1
                    end
                end
                local function sum(n)
                    if n == 0 then return 0 end
                    return 1 + sum(n - 1)
                end
                return failures, sum(20)
