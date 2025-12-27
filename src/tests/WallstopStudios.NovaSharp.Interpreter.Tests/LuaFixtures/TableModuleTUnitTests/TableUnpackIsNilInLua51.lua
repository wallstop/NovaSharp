-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:479
-- @test: TableModuleTUnitTests.TableUnpackIsNilInLua51
-- @compat-notes: Test targets Lua 5.1
return table.unpack
