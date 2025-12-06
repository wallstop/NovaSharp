-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:91
-- @test: IoModuleVirtualizationTUnitTests.OsTmpNameGeneratesUniqueVirtualNames
return os.tmpname()
