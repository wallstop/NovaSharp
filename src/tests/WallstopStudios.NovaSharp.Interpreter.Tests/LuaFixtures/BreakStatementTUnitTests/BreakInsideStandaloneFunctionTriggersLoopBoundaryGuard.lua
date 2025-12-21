-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/Statements/BreakStatementTUnitTests.cs:45
-- @test: BreakStatementTUnitTests.BreakInsideStandaloneFunctionTriggersLoopBoundaryGuard
local function inner()
                    break
                end
