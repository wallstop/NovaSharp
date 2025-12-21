-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:446
-- @test: TableModuleTUnitTests.PackAvailableInLua52Plus
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: table.pack (5.2+)
return table.pack(1, 2, 3).n
