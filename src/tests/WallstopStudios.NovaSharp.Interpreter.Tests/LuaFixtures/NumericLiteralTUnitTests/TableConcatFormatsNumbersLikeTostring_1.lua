-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs:253
-- @test: NumericLiteralTUnitTests.TableConcatFormatsNumbersLikeTostring
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder
return tostring({expression})
