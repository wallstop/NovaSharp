-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:138
-- @test: OsTimeModuleTUnitTests.DiffTimeHandlesOptionalStartArgument
return os.difftime(200, 150)
