-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:203
-- @test: TableTUnitTests.TableSimplifiedAccesses
-- @compat-notes: Lua 5.3+: bitwise operators
t = { ciao = 'hello' }
