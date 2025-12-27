-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:175
-- @test: TableTUnitTests.TableIPairsStopsAfterBreak
x = 0
                y = 0
                t = { 2, 4, 6, 8, 10, 12 }
                for i,j in ipairs(t) do
                    x = x + i
                    y = y + j
                    if (i >= 3) then break end
                end
                return x, y
