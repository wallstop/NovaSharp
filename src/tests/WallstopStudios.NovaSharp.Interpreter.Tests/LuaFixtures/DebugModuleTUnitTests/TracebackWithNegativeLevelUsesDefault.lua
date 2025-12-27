-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3178
-- @test: DebugModuleTUnitTests.TracebackWithNegativeLevelUsesDefault
-- @compat-notes: Test targets Lua 5.1
local function test()
                    return debug.traceback('msg', -1)
                end
                return test()
