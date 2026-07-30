-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCompilationCacheTUnitTests.cs:1509
-- @test: ScriptCompilationCacheTUnitTests.DoStringAfterClearRecompilesInsteadOfUsingMostRecentLookup
-- Compatibility notes: Test targets Lua 5.4+
counter = (counter or 0) + 1; return counter
