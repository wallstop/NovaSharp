-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoStdHandleUserDataTUnitTests.cs:37
-- @test: IoStdHandleUserDataTUnitTests.StdInEqualsItselfButNotStdOut
-- @compat-notes: Lua 5.3+: bitwise operators
return io.stdin == io.stdin, io.stdin ~= io.stdout, io.stdin == 1, io.stdin ~= 1
