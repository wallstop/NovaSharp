-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/DynValueZStringTUnitTests.cs:209
-- @test: DynValueZStringTUnitTests.TableConcatUsesZStringInternally
local t = {'a', 'b', 'c', 'd'}
                return table.concat(t)
