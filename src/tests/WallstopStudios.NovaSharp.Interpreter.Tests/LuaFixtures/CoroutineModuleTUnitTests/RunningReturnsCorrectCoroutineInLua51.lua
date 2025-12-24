-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1301
-- @test: CoroutineModuleTUnitTests.RunningReturnsCorrectCoroutineInLua51
-- @compat-notes: Test targets Lua 5.1
local function checkRunning()
                    local running = coroutine.running()
                    -- Should be a thread value, not nil
                    return type(running)
                end
                
                local co = coroutine.create(checkRunning)
                local ok, result = coroutine.resume(co)
                return result
