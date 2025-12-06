-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:124
-- @test: ErrorHandlingModuleTUnitTests.PcallWrapsClrTailCallRequestWithoutHandlers
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, value = pcall(tailing)
                return { ok = ok, value = value, valueType = type(value) }
