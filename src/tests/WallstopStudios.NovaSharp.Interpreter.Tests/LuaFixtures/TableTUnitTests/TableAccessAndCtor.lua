-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:33
-- @test: TableTUnitTests.TableAccessAndCtor
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
