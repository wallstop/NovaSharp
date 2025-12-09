-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:19
-- @test: ErrorHandlingModuleTUnitTests.PcallReturnsAllValuesOnSuccess
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, a, b = pcall(function() return 1, 2 end)
                return ok, a, b
