-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/Statements/BreakStatementTUnitTests.cs:108
-- @test: BreakStatementTUnitTests.MultipleBreakStatementsRespectInnermostLoopScope
local log = {}

                local function emit(flag)
                    while true do
                        if flag then
                            table.insert(log, 'first')
                            break
                        end

                        table.insert(log, 'second')
                        break
                    end
                end

                emit(true)
                emit(false)
                return log
