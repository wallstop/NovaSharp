-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3145
-- @test: DebugModuleTUnitTests.TracebackWithNumberMessageConvertsToString
-- @compat-notes: Test targets Lua 5.1
local function test()
                    return debug.traceback(42, 1)
                end
                return test()
