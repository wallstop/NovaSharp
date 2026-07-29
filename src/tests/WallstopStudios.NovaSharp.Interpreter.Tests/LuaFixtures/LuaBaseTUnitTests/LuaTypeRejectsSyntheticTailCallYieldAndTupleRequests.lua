-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LuaBaseTUnitTests.cs:97
-- @test: LuaBaseTUnitTests.LuaTypeRejectsSyntheticTailCallYieldAndTupleRequests
return (function() return 1, 'two' end)()
