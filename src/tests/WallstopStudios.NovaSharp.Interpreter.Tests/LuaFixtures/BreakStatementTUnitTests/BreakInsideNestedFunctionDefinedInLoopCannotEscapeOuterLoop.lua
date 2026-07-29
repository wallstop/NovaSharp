-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/Statements/BreakStatementTUnitTests.cs:71
-- @test: BreakStatementTUnitTests.BreakInsideNestedFunctionDefinedInLoopCannotEscapeOuterLoop
for i = 1, 2 do
                    local function inner()
                        break
                    end
                end
