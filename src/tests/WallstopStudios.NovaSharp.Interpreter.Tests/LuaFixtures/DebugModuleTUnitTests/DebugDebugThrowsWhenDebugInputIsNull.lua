-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:472
-- @test: DebugModuleTUnitTests.DebugDebugThrowsWhenDebugInputIsNull
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function() debug.debug() end)
                return ok, err
