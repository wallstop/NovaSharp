-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:327
-- @test: TailCallTUnitTests.DebugGetInfoDefaultOmitsTailCallFlagInLua51
-- Compatibility notes: Test targets Lua 5.1
local info = debug.getinfo(1)
assert(info.istailcall == nil)
