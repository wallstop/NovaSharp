-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:289
-- @test: TableTUnitTests.PrimeTableAllowsSimpleValues
-- @compat-notes: Prime table syntax (${ }) is NovaSharp-specific, not standard Lua
t = ${ ciao = 'hello' }
