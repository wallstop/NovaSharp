-- @lua-versions: 5.2, 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/Bit32CompatibilityWarningTUnitTests.cs:56
-- @test: Bit32CompatibilityWarningTUnitTests.RequireBit32InLua52DoesNotEmitWarning
-- @compat-notes: Test targets Lua 5.2+
return require('bit32') ~= nil
