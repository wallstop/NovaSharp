-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:509
-- @test: TableModuleTUnitTests.TableUnpackIsNilInLua51
-- Compatibility notes: Test targets Lua 5.1
return table.unpack
