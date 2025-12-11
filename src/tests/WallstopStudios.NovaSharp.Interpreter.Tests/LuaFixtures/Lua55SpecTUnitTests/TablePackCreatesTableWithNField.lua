-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:200
-- @test: Lua55SpecTUnitTests.TablePackCreatesTableWithNField
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.2+: table.pack (5.2+)
local t = table.pack(10, 20, 30)
                return t.n, t[1], t[2], t[3]
