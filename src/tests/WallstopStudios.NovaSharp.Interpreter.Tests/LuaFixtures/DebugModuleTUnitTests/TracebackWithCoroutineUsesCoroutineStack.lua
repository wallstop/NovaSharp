-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:972
-- @test: DebugModuleTUnitTests.TracebackWithCoroutineUsesCoroutineStack
-- @compat-notes: Lua 5.3+: bitwise operators
local function inner()
                    return debug.traceback(coroutine.running(), 'message')
                end
                local co = coroutine.create(inner)
                local ok, trace = coroutine.resume(co)
                return ok, trace
