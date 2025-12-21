-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:269
-- @test: ErrorHandlingModuleTUnitTests.XpcallReturnsSuccessWhenFunctionSucceeds
local function succeed()
                    return 'done', 42
                end

                local function handler(msg)
                    return 'handled:' .. msg
                end

                return xpcall(succeed, handler)
