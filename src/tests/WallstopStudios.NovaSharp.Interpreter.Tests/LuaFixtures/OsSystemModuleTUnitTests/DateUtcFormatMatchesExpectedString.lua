-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:268
-- @test: OsSystemModuleTUnitTests.DateUtcFormatMatchesExpectedString
return os.date('!%d/%m/%y %H:%M:%S', 0)
