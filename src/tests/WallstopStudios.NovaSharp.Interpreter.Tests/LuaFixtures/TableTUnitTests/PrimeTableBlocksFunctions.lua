-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:304
-- @test: TableTUnitTests.PrimeTableBlocksFunctions
-- @compat-notes: Prime table syntax (${ }) is NovaSharp-specific, not standard Lua
t = ${ ciao = function() end }
