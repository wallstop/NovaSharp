-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\TableTUnitTests.cs:289
-- @test: TableTUnitTests.PrimeTableAllowsSimpleValues
-- @compat-notes: NovaSharp: NovaSharp prime table syntax; Test targets Lua 5.2+
t = ${ ciao = 'hello' }
