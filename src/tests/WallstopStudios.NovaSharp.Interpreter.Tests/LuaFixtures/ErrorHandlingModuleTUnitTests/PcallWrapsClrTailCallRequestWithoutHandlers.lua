-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:135
-- @test: ErrorHandlingModuleTUnitTests.PcallWrapsClrTailCallRequestWithoutHandlers
local ok, value = pcall(tailing)
                return { ok = ok, value = value, valueType = type(value) }
