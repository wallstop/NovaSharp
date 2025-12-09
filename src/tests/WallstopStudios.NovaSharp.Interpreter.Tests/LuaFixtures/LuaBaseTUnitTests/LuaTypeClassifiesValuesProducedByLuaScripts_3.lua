-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LuaBaseTUnitTests.cs:117
-- @test: LuaBaseTUnitTests.LuaTypeClassifiesValuesProducedByLuaScripts
return function() return 'noop' end
