-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:304
-- @test: TableTUnitTests.PrimeTableBlocksFunctions
t = ${ ciao = function() end }
