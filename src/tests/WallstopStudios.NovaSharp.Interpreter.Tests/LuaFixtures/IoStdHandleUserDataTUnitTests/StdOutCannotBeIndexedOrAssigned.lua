-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:130
-- @test: IoStdHandleUserDataTUnitTests.StdOutCannotBeIndexedOrAssigned
return io.stdout[1]
