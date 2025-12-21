-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/DynamicTUnitTests.cs:101
-- @test: DynamicTUnitTests.DynamicAccessFromCSharp
-- @compat-notes: Test targets Lua 5.1
t = { ciao = { 'hello' } }
