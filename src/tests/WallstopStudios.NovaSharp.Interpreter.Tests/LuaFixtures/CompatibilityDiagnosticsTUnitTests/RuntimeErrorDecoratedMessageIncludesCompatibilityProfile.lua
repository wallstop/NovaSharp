-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/CompatibilityDiagnosticsTUnitTests.cs:36
-- @test: CompatibilityDiagnosticsTUnitTests.RuntimeErrorDecoratedMessageIncludesCompatibilityProfile
-- @compat-notes: Test targets Lua 5.2+
error('boom')
