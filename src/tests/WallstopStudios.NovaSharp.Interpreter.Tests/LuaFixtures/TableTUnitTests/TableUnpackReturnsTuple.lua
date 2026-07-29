-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:357
-- @test: TableTUnitTests.TableUnpackReturnsTuple
-- Compatibility notes: Test targets Lua 5.2+; Lua 5.2+: table.unpack (5.2+)
return table.unpack({3,4})
