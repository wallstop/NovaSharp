-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoStdHandleUserDataTUnitTests.cs:92
-- @test: IoStdHandleUserDataTUnitTests.IoInputReturnsCurrentStdInHandle
-- @compat-notes: Lua 5.3+: bitwise operators
return io.input() == io.stdin
