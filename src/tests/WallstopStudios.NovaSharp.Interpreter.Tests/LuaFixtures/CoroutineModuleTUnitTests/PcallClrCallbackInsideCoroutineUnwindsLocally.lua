-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1391
-- @test: CoroutineModuleTUnitTests.PcallClrCallbackInsideCoroutineUnwindsLocally
-- Compatibility notes: Test targets Lua 5.4+
local task = coroutine.create(function()
                    local ok, err = pcall(string.gsub, 'abc', 'b', function()
                        error('rep', 0)
                    end)
                    return ok, err
                end)

                local resumed, ok, err = coroutine.resume(task)

                print('resume', resumed, ok, err)

                return resumed, ok, err
