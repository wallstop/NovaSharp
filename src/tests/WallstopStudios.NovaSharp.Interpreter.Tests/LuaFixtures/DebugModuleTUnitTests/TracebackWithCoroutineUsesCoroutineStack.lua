-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1402
-- @test: DebugModuleTUnitTests.TracebackWithCoroutineUsesCoroutineStack
-- @compat-notes: Test targets Lua 5.1
local function inner()
                    return debug.traceback(coroutine.running(), 'message')
                end
                local co = coroutine.create(inner)
                local ok, trace = coroutine.resume(co)
                return ok, trace
