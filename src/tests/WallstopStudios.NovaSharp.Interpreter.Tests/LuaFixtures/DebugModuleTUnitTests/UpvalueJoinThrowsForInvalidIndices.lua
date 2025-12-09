-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:449
-- @test: DebugModuleTUnitTests.UpvalueJoinThrowsForInvalidIndices
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.2+: debug.upvaluejoin (5.2+)
local function f() end
                local ok, err = pcall(function() debug.upvaluejoin(f, 999, f, 1) end)
                return ok, err
