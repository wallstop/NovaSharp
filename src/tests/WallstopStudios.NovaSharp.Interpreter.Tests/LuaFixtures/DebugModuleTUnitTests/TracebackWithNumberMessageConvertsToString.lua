-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3214
-- @test: DebugModuleTUnitTests.TracebackWithNumberMessageConvertsToString
-- Compatibility notes: Test targets Lua 5.1
local function test()
                    return debug.traceback(42, 1)
                end
                return test()
