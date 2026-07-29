-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3177
-- @test: DebugModuleTUnitTests.TracebackWithEmptyMessageWorks
-- Compatibility notes: Test targets Lua 5.1
local function test()
                    return debug.traceback('')
                end
                return test()
