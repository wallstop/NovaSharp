-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:42
-- @test: ErrorHandlingModuleTUnitTests.PcallContinuationPreservesReturnArity
local noneOk, noneValue = pcall(function() end)
                local oneOk, oneValue, oneExtra = pcall(function() return 42 end)
                local manyOk, first, second, third = pcall(function() return 1, 2, 3 end)
                return
                    noneOk,
                    noneValue == nil,
                    oneOk,
                    oneValue,
                    oneExtra == nil,
                    manyOk,
                    first,
                    second,
                    third
