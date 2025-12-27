-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsExecuteVersionParityTUnitTests.cs:219
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteWithNoArgsReturnsTrueAllVersions
return os.execute()
