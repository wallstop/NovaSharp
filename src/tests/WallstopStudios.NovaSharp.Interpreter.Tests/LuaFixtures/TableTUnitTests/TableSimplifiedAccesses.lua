-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:182
-- @test: TableTUnitTests.TableSimplifiedAccesses
-- @compat-notes: Lua 5.3+: bitwise operators
t = { ciao = 'hello' } return t
