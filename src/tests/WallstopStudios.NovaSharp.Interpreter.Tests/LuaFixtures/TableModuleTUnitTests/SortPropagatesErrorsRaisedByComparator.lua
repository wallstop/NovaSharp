-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:189
-- @test: TableModuleTUnitTests.SortPropagatesErrorsRaisedByComparator
-- @compat-notes: Test targets Lua 5.1
local values = { 1, 2 }
                    table.sort(values, function()
                        error('sort failed')
                    end)
