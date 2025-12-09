-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxRecursionLimitTUnitTests.cs:52
-- @test: SandboxRecursionLimitTUnitTests.ScriptExecutesWithinRecursionLimit
-- @compat-notes: Lua 5.3+: bitwise operators
function factorial(n)
                    if n <= 1 then
                        return 1
                    end
                    return n * factorial(n - 1)
                end
