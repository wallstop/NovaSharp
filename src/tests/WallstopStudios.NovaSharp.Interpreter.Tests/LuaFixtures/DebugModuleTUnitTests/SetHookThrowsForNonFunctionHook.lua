-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1770
-- @test: DebugModuleTUnitTests.SetHookThrowsForNonFunctionHook
-- @compat-notes: Test targets Lua 5.1
local ok, err = pcall(function() debug.sethook('not a function', 'c') end)
                return ok, err
