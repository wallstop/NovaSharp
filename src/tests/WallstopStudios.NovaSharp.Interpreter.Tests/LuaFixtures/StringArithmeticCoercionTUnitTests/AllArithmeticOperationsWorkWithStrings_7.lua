-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:183
-- @test: StringArithmeticCoercionTUnitTests.AllArithmeticOperationsWorkWithStrings
-- Compatibility notes: Test targets Lua 5.1
local addOk, addResult = pcall(function() return 'Infinity' + 0 end)
                local unaryOk, unaryResult = pcall(function() return -'Infinity' end)
                return addOk, addResult, unaryOk, unaryResult
