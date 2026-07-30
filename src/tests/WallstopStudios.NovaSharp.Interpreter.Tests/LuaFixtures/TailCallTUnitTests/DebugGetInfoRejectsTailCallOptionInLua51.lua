-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:310
-- @test: TailCallTUnitTests.DebugGetInfoRejectsTailCallOptionInLua51
-- Compatibility notes: Test targets Lua 5.1
return debug.getinfo(1, 't')
