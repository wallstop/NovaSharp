-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/CompatibilityDiagnosticsTUnitTests.cs:19
-- @test: CompatibilityDiagnosticsTUnitTests.SyntaxErrorDecoratedMessageIncludesCompatibilityProfile
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: bitwise operators
local = 1
