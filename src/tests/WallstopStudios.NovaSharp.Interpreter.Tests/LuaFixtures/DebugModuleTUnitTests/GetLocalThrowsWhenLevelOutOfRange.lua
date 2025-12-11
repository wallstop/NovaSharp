-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:123
-- @test: DebugModuleTUnitTests.GetLocalThrowsWhenLevelOutOfRange
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function() debug.getlocal(128, 1) end)
                return ok, err
