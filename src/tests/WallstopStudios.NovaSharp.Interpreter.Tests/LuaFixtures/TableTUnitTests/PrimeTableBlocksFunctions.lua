-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:381
-- @test: TableTUnitTests.PrimeTableBlocksFunctions
-- Compatibility notes: NovaSharp: NovaSharp prime table syntax
t = ${ ciao = function() end }
