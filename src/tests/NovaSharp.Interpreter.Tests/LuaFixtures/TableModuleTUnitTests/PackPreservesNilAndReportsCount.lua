-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:16
-- @test: TableModuleTUnitTests.PackPreservesNilAndReportsCount
-- @compat-notes: Lua 5.3+: bitwise operators
local t = table.pack('a', nil, 42)
                return t.n, t[1], t[2], t[3]
