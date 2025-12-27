-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:572
-- @test: DebugModuleTUnitTests.SetUserValueThrowsWhenNoValueProvided
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.setuservalue (5.2+)
local ok, err = pcall(function() debug.setuservalue(ud) end)
                return ok, err
