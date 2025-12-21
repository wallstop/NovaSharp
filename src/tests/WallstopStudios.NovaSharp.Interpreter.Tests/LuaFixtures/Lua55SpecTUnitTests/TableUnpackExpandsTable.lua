-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:235
-- @test: Lua55SpecTUnitTests.TableUnpackExpandsTable
-- @compat-notes: Lua 5.2+: table.unpack (5.2+)
local function sum3(a, b, c) return a + b + c end
                return sum3(table.unpack({10, 20, 30}))
