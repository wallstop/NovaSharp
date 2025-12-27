-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/BinaryDumpTUnitTests.cs:22
-- @test: BinaryDumpTUnitTests.BinDumpChunkDump
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: load with string arg (5.2+)
local chunk = load('return 81;');
