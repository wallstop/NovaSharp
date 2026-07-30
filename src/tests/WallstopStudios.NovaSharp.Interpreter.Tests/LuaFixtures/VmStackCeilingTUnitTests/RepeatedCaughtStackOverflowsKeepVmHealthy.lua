-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/VmStackCeilingTUnitTests.cs:173
-- @test: VmStackCeilingTUnitTests.RepeatedCaughtStackOverflowsKeepVmHealthy
-- Recovers from 50 overflows at NovaSharp's configurable ceiling; wall-clock tracks that ceiling rather than Lua semantics, so comparing against reference Lua measures each engine's own depth limit and times out non-deterministically
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
