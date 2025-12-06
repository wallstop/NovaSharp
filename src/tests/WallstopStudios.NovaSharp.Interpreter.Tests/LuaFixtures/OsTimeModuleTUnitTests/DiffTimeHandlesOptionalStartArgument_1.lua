-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:139
-- @test: OsTimeModuleTUnitTests.DiffTimeHandlesOptionalStartArgument
return os.difftime(200)
