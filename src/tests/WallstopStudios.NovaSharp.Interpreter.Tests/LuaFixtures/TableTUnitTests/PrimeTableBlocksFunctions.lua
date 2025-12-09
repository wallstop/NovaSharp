-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\TableTUnitTests.cs:266
-- @test: TableTUnitTests.PrimeTableBlocksFunctions
-- @compat-notes: Lua 5.3+: bitwise operators
t = ${ ciao = function() end }
