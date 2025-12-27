-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:21
-- @test: ErrorHandlingModuleTUnitTests.PcallReturnsAllValuesOnSuccess
local ok, a, b = pcall(function() return 1, 2 end)
                return ok, a, b
