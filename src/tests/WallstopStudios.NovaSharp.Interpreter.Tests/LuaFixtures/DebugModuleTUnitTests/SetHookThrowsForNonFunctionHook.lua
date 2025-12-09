-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1102
-- @test: DebugModuleTUnitTests.SetHookThrowsForNonFunctionHook
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function() debug.sethook('not a function', 'c') end)
                return ok, err
