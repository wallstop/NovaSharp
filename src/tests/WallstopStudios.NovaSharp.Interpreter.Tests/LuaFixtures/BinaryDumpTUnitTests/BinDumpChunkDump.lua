-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\BinaryDumpTUnitTests.cs:22
-- @test: BinaryDumpTUnitTests.BinDumpChunkDump
-- Test targets Lua 5.2+; Lua 5.2+: load with string arg (5.2+)
local chunk = load('return 81;');
