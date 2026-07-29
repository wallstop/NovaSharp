-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCompilationCacheTUnitTests.cs:1522
-- @test: ScriptCompilationCacheTUnitTests.DoStringAfterClearRecompilesInsteadOfUsingMostRecentLookup
counter = (counter or 0) + 1; return counter
