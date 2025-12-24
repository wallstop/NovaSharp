-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:572
-- @test: TableModuleTUnitTests.MaxnAvailableInLua51And52
-- @compat-notes: Test targets Lua 5.1
return table.maxn({[5] = true, [3] = true})
