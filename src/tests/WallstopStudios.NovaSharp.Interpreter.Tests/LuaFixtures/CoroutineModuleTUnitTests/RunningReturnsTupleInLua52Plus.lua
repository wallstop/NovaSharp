-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1387
-- @test: CoroutineModuleTUnitTests.RunningReturnsTupleInLua52Plus
-- @compat-notes: Test targets Lua 5.1
-- Count how many values coroutine.running returns
                local function countReturns()
                    return select('#', coroutine.running())
                end
                
                local co = coroutine.create(countReturns)
                local ok, count = coroutine.resume(co)
                return count
