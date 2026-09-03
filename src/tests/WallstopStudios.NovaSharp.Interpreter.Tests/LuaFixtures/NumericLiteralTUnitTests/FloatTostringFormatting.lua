-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs
-- @test: NumericLiteralTUnitTests.FloatsFormatLikeReferenceTostring
-- Lua 5.1-5.4 print floats with %.14g; Lua 5.5 starts from %.15g and falls
-- back to %.17g when the shorter form does not round-trip.
print(1/3)
print(1e15)
print(2^53)
print(2.0)
print(-0.0)
print(0.1)
print(1e14)
print(123456789012345.0)
print(1234567.891234567)
print(1.0000000000000002)
print(4.9e-324)
print(1/3 .. "")
print(2^53 .. "|" .. 1e14)
