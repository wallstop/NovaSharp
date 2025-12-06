-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:82
-- @test: ErrorHandlingModuleTUnitTests.PcallForwardsArgumentsToScriptFunction
-- @compat-notes: Lua 5.3+: bitwise operators
local function sum(a, b, c)
                    return a + b + c
                end

                local ok, value = pcall(sum, 2, 4, 8)
                return ok, value
