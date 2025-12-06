-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:122
-- @test: DebugModuleTUnitTests.GetLocalThrowsWhenLevelOutOfRange
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function() debug.getlocal(128, 1) end)
                return ok, err
