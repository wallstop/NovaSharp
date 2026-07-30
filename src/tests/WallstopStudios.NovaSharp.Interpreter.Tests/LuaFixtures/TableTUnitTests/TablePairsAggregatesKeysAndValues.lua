-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:152
-- @test: TableTUnitTests.TablePairsAggregatesKeysAndValues
V = 0
                K = ''
                t = { a = 1, b = 2, c = 3, d = 4, e = 5 }
                for k, v in pairs(t) do
                    K = K .. k
                    V = V + v
                end
                return K, V
