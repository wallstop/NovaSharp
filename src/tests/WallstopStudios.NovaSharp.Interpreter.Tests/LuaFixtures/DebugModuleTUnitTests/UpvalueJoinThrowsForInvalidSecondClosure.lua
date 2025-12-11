-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:1286
-- @test: DebugModuleTUnitTests.UpvalueJoinThrowsForInvalidSecondClosure
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.2+: debug.upvaluejoin (5.2+)
local x = 1
                local function f1() return x end
                local function f2() end
                local ok, err = pcall(function() debug.upvaluejoin(f1, 1, f2, 999) end)
                return ok, err
