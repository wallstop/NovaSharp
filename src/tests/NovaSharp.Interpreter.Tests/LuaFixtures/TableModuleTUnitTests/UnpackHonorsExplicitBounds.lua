-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:34
-- @test: TableModuleTUnitTests.UnpackHonorsExplicitBounds
-- @compat-notes: Lua 5.3+: bitwise operators
local values = { 10, 20, 30, 40 }
                return table.unpack(values, 2, 3)
