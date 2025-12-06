-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorStackTraceTUnitTests.cs:58
-- @test: ProcessorStackTraceTUnitTests.InterpreterExceptionIncludesCallStackFrames
local function level3()
                    return missing_function()
                end

                local function level2()
                    return level3()
                end

                function level1()
                    return level2()
                end
