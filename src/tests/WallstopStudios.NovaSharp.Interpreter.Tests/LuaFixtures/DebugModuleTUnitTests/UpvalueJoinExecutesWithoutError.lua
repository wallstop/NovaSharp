-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:688
-- @test: DebugModuleTUnitTests.UpvalueJoinExecutesWithoutError
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.upvaluejoin (5.2+)
local x = 1
                local y = 2
                local function f1() return x end
                local function f2() return y end
                debug.upvaluejoin(f1, 1, f2, 1)
                return f1(), f2()
