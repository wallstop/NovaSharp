-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:477
-- @test: Lua55SpecTUnitTests.SelectWithHashReturnsArgumentCount
return select('#', 'a', 'b', 'c')
