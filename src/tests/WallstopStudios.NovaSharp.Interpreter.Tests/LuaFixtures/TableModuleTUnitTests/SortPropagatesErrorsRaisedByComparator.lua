-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:150
-- @test: TableModuleTUnitTests.SortPropagatesErrorsRaisedByComparator
-- @compat-notes: Lua 5.3+: bitwise operators
local values = { 1, 2 }
                    table.sort(values, function()
                        error('sort failed')
                    end)
