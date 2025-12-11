-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:140
-- @test: OsTimeModuleTUnitTests.DiffTimeHandlesOptionalStartArgument
return os.difftime(200)
