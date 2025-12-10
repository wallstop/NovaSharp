-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\TableTUnitTests.cs:252
-- @test: TableTUnitTests.PrimeTableAllowsSimpleValues
-- @compat-notes: NovaSharp extension - Prime table syntax (${ }) is a NovaSharp-specific feature
t = ${ ciao = 'hello' }
