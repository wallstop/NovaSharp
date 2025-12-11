-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:1335
-- @test: DebugModuleTUnitTests.TracebackWithNilLevelUsesDefault
-- @compat-notes: Lua 5.2+: debug.traceback with nil level (5.2+)
local function inner()
                    return debug.traceback('msg', nil)
                end
                return inner()
