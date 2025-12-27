-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxRecursionLimitTUnitTests.cs:62
-- @test: SandboxRecursionLimitTUnitTests.ScriptExecutesWithinRecursionLimit
-- @compat-notes: Test class 'SandboxRecursionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
function factorial(n)
                    if n <= 1 then
                        return 1
                    end
                    return n * factorial(n - 1)
                end
