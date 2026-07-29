-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorStackTraceTUnitTests.cs:64
-- @test: ProcessorStackTraceTUnitTests.InterpreterExceptionIncludesCallStackFrames
local function level3()
                    return missing_function()
                end

                local function level2()
                    local value = level3()
                    return value
                end

                function level1()
                    local value = level2()
                    return value
                end
