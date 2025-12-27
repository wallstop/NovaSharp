-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2054
-- @test: DebugModuleTUnitTests.TracebackWithNumberLevel
-- @compat-notes: Test targets Lua 5.1
local function deep()
                    return debug.traceback('trace', 2)
                end
                local function middle()
                    return deep()
                end
                local function outer()
                    return middle()
                end
                return outer()
