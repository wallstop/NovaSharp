-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxRecursionLimitTUnitTests.cs:162
-- @test: SandboxRecursionLimitTUnitTests.MutualRecursionCountedCorrectly
-- @compat-notes: Lua 5.3+: bitwise operators
function isEven(n)
                    if n == 0 then return true end
                    local result = isOdd(n - 1)
                    return result  -- Local variable prevents TCO
                end
                
                function isOdd(n)
                    if n == 0 then return false end
                    local result = isEven(n - 1)
                    return result  -- Local variable prevents TCO
                end
