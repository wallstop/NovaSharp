-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:135
-- @test: IoStdHandleUserDataTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
io.stdout[1] = 42
