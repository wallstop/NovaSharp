-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:392
-- @test: DebugModuleTUnitTests.TracebackReturnsCallStack
local function inner()
                    return debug.traceback()
                end
                local function outer()
                    return inner()
                end
                return outer()
