-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:252
-- @test: TableTUnitTests.PrimeTableAllowsSimpleValues
-- @compat-notes: Prime tables (${ }) are a NovaSharp-only extension, not standard Lua

-- Test prime table syntax (NovaSharp extension)
t = ${ ciao = 'hello' }
if t.ciao == 'hello' then
    print("PASS")
else
    error("Expected prime table to work")
end
