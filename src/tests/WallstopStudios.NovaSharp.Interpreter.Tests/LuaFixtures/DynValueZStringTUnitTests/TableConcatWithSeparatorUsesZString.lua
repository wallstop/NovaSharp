-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/DynValueZStringTUnitTests.cs:215
-- @test: DynValueZStringTUnitTests.TableConcatWithSeparatorUsesZString
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {'a', 'b', 'c', 'd'}
                return table.concat(t, ', ')
