-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\TableTUnitTests.cs:280
-- @test: TableTUnitTests.TableUnpackReturnsTuple
-- Test targets Lua 5.2+; Lua 5.2+: table.unpack (5.2+)
return table.unpack({3,4})
