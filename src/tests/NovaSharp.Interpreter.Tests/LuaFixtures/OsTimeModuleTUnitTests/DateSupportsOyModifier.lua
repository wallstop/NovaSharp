-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:212
-- @test: OsTimeModuleTUnitTests.DateSupportsOyModifier
return os.date('!%Oy', 0)
