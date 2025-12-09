-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:18
-- @test: ParserTUnitTests.SyntaxErrorsIncludeLineInformation
-- @compat-notes: Lua 5.3+: bitwise operators
function broken()
                        local x =
                    end
