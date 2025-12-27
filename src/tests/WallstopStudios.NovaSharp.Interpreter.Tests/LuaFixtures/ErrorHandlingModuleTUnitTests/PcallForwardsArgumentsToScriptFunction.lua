-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:89
-- @test: ErrorHandlingModuleTUnitTests.PcallForwardsArgumentsToScriptFunction
local function sum(a, b, c)
                    return a + b + c
                end

                local ok, value = pcall(sum, 2, 4, 8)
                return ok, value
