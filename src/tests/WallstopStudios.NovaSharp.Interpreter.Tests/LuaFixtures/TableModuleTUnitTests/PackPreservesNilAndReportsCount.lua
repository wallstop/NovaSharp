-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:21
-- @test: TableModuleTUnitTests.PackPreservesNilAndReportsCount
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: table.pack (5.2+)
local t = table.pack('a', nil, 42)
                return t.n, t[1], t[2], t[3]
