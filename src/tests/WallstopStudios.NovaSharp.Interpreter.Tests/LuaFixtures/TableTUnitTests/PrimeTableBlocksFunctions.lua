-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:304
-- @test: TableTUnitTests.PrimeTableBlocksFunctions
-- @compat-notes: NovaSharp: NovaSharp prime table syntax
t = ${ ciao = function() end }
