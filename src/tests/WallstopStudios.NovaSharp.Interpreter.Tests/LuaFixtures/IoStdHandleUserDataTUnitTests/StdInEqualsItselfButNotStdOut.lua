-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:41
-- @test: IoStdHandleUserDataTUnitTests.StdInEqualsItselfButNotStdOut
return io.stdin == io.stdin, io.stdin ~= io.stdout, io.stdin == 1, io.stdin ~= 1
