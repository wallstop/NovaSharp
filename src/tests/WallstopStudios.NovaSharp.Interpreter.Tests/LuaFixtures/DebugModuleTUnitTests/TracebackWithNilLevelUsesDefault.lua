-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1859
-- @test: DebugModuleTUnitTests.TracebackWithNilLevelUsesDefault
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.traceback with nil level (5.2+)
local function inner()
                    return debug.traceback('msg', nil)
                end
                return inner()
