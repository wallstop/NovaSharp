-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/DynamicTUnitTests.cs:70
-- @test: DynamicTUnitTests.DynamicAccessFromCSharp
-- @compat-notes: Lua 5.3+: bitwise operators
t = { ciao = { 'hello' } }
