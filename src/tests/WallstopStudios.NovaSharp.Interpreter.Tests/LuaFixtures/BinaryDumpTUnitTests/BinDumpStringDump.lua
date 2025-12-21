-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/BinaryDumpTUnitTests.cs:36
-- @test: BinaryDumpTUnitTests.BinDumpStringDump
local str = string.dump(function(n) return n * n; end);
