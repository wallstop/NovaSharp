-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:266
-- @test: TableTUnitTests.PrimeTableBlocksFunctions
-- @compat-notes: Lua 5.3+: bitwise operators
t = ${ ciao = function() end }
