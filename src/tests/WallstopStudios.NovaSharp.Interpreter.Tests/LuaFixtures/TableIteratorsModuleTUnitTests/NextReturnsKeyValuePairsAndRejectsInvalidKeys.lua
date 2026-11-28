-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableIteratorsModuleTUnitTests.cs:102
-- @test: TableIteratorsModuleTUnitTests.NextReturnsKeyValuePairsAndRejectsInvalidKeys
local values = { first = 1 }
                local key, value = next(values)
                local key2, value2 = next(values, key)
                local invalidOk, invalidError = pcall(next, values, 'missing')
                return key, value, key2, value2, invalidOk, invalidError
