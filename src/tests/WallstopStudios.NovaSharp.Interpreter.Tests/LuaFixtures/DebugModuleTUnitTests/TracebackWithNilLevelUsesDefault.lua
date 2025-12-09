-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1317
-- @test: DebugModuleTUnitTests.TracebackWithNilLevelUsesDefault
local function inner()
                    return debug.traceback('msg', nil)
                end
                return inner()
