-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:415
-- @test: DebugModuleTUnitTests.UpvalueJoinExecutesWithoutError
-- @compat-notes: Lua 5.3+: bitwise operators
local x = 1
                local y = 2
                local function f1() return x end
                local function f2() return y end
                debug.upvaluejoin(f1, 1, f2, 1)
                return f1(), f2()
