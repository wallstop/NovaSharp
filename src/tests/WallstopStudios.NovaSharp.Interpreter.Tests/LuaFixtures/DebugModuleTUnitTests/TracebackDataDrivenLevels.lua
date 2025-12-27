-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2765
-- @test: DebugModuleTUnitTests.TracebackDataDrivenLevels
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local function level3()
                    return debug.traceback('marker', {level})
                end
                local function level2()
                    return level3()
                end
                local function level1()
                    return level2()
                end
                return level1()
