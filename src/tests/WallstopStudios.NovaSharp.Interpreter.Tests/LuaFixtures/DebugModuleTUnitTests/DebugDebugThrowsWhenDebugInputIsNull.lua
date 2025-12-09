-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:485
-- @test: DebugModuleTUnitTests.DebugDebugThrowsWhenDebugInputIsNull
-- @compat-notes: NovaSharp: debug.debug() is interactive/platform-dependent
local ok, err = pcall(function() debug.debug() end)
                return ok, err
