-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxRecursionLimitTUnitTests.cs:109
-- @test: SandboxRecursionLimitTUnitTests.RecursionLimitCallbackCanAllowContinuation
-- @compat-notes: Lua 5.3+: bitwise operators
function recurse(n)
                    if n <= 0 then
                        return 0
                    end
                    return 1 + recurse(n - 1)
                end
