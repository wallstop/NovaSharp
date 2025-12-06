-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:37
-- @test: IoStdHandleUserDataTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
return io.stdin == io.stdin, io.stdin ~= io.stdout, io.stdin == 1, io.stdin ~= 1
