-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LuaBaseTUnitTests.cs:118
-- @test: LuaBaseTUnitTests.LuaTypeClassifiesValuesProducedByLuaScripts
return coroutine.create(function() coroutine.yield('hi') end)
