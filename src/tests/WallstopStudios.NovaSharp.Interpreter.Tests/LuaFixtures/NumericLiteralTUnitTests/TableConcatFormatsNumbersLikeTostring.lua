-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs:236
-- @test: NumericLiteralTUnitTests.TableConcatFormatsNumbersLikeTostring
-- Compatibility notes: Test targets Lua 5.2+
return table.concat({2.0, -5.0, 3.5, 1e100, 0.1, 1/3, 1e14, 2^53}, ",")
