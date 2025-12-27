-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:858
-- @test: DebugModuleTUnitTests.TracebackReturnsCallStack
-- @compat-notes: Test targets Lua 5.1
local function inner()
                    return debug.traceback()
                end
                local function outer()
                    return inner()
                end
                return outer()
