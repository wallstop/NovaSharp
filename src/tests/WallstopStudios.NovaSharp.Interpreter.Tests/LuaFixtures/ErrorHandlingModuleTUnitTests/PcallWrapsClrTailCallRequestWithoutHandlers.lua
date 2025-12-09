-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:125
-- @test: ErrorHandlingModuleTUnitTests.PcallWrapsClrTailCallRequestWithoutHandlers
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, value = pcall(tailing)
                return { ok = ok, value = value, valueType = type(value) }
