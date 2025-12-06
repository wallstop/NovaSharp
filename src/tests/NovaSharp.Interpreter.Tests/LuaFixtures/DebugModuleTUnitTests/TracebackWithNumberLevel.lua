-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1292
-- @test: DebugModuleTUnitTests.TracebackWithNumberLevel
local function deep()
                    return debug.traceback('trace', 2)
                end
                local function middle()
                    return deep()
                end
                local function outer()
                    return middle()
                end
                return outer()
