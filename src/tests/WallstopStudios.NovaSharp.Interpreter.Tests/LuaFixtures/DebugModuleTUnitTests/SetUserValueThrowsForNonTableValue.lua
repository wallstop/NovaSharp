-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:293
-- @test: DebugModuleTUnitTests.SetUserValueThrowsForNonTableValue
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function() debug.setuservalue(ud, 'not a table') end)
                return ok, err
