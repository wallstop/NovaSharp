-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:460
-- @test: TableModuleTUnitTests.TableUnpackAvailableInLua52Plus
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: table.unpack (5.2+)
return table.unpack({10, 20, 30})
