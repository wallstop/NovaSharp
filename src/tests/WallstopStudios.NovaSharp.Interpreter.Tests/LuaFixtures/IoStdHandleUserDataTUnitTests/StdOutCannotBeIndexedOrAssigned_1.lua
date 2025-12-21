-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:146
-- @test: IoStdHandleUserDataTUnitTests.StdOutCannotBeIndexedOrAssigned
io.stdout[1] = 42
