-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:267
-- @test: TableTUnitTests.NilRemovesEntryForPairs
-- @compat-notes: Test targets Lua 5.2+
str = ''
                function showTable(t)
                    for i,j in pairs(t) do
                        str = str .. i
                    end
                    str = str .. '$'
                end
                tb = {}
                tb['id'] = 3
                showTable(tb)
                tb['id'] = nil
                showTable(tb)
                return str
