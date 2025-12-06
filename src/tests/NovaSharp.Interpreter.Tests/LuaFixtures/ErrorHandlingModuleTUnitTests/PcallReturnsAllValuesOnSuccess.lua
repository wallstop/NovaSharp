-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:18
-- @test: ErrorHandlingModuleTUnitTests.PcallReturnsAllValuesOnSuccess
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, a, b = pcall(function() return 1, 2 end)
                return ok, a, b
