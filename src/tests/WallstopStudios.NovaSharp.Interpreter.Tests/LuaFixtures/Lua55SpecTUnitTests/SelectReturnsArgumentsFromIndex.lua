-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:507
-- @test: Lua55SpecTUnitTests.SelectReturnsArgumentsFromIndex
-- @compat-notes: Test targets Lua 5.5+
return select(2, 'a', 'b', 'c')
