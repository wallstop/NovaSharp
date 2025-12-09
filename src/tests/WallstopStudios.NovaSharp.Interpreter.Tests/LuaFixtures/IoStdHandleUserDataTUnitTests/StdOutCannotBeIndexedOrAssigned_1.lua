-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoStdHandleUserDataTUnitTests.cs:135
-- @test: IoStdHandleUserDataTUnitTests.StdOutCannotBeIndexedOrAssigned
-- @compat-notes: Lua 5.3+: bitwise operators
io.stdout[1] = 42
