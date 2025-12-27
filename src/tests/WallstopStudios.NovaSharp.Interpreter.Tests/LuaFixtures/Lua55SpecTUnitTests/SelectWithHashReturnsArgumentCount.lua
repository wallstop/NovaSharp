-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:518
-- @test: Lua55SpecTUnitTests.SelectWithHashReturnsArgumentCount
-- @compat-notes: Test targets Lua 5.5+
return select('#', 'a', 'b', 'c')
