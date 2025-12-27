-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/Statements/BreakStatementTUnitTests.cs:133
-- @test: BreakStatementTUnitTests.BreakOnlyExitsInnermostLoop
local log = {}
                for outer = 1, 3 do
                    for inner = 1, 3 do
                        table.insert(log, string.format('%d-%d', outer, inner))
                        break
                    end
                end
                return log
