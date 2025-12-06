-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:465
-- @test: OsSystemModuleTUnitTests.TmpNameReturnsQueuedPlatformValuesThenFallsBack
return os.tmpname(), os.tmpname(), os.tmpname()
