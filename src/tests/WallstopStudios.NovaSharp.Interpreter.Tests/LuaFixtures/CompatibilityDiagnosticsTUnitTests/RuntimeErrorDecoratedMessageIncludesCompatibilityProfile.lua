-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\CompatibilityDiagnosticsTUnitTests.cs:39
-- @test: CompatibilityDiagnosticsTUnitTests.RuntimeErrorDecoratedMessageIncludesCompatibilityProfile
-- @compat-notes: Test targets Lua 5.2+
error('boom')
