-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:100
-- @test: IoStdHandleUserDataTUnitTests.IoInputReturnsCurrentStdInHandle
return io.input() == io.stdin
