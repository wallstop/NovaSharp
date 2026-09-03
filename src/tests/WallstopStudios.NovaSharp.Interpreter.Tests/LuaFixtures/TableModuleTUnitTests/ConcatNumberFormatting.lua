-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs
-- @test: NumericLiteralTUnitTests.TableConcatFormatsNumbersLikeTostring
-- table.concat renders numbers exactly like tostring in every version.
print(table.concat({2.0, -5.0, 3.5, 1e100, 0.1, 1/3, 1e14, 2^53}, ","))
print(table.concat({1, 2, 3}))
print(table.concat({0x10, 0xff}, "-"))
print(table.concat({-0.0, 1.0}))
print(2.0 .. "|" .. 1e15 .. "|" .. 1/3)
