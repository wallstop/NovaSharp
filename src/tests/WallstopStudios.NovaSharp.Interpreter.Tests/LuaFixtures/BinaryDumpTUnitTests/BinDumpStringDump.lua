-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/BinaryDumpTUnitTests.cs:36
-- @test: BinaryDumpTUnitTests.BinDumpStringDump
-- @compat-notes: Test targets Lua 5.2+
local str = string.dump(function(n) return n * n; end);
