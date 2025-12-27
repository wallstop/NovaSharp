-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxRecursionLimitTUnitTests.cs:29
-- @test: SandboxRecursionLimitTUnitTests.RecursionLimitExceededThrowsSandboxViolationException
-- @compat-notes: Test class 'SandboxRecursionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
function recurse(n)
                    if n <= 0 then
                        return 0
                    end
                    return 1 + recurse(n - 1)
                end
