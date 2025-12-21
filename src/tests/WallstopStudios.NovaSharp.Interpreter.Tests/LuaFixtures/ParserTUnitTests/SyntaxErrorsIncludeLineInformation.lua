-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:23
-- @test: ParserTUnitTests.SyntaxErrorsIncludeLineInformation
function broken()
                        local x =
                    end
