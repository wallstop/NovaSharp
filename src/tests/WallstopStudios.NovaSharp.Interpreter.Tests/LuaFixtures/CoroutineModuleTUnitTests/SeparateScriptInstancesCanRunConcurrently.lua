-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1241
-- @test: CoroutineModuleTUnitTests.SeparateScriptInstancesCanRunConcurrently
waitForProceed()
                            local sum = 0
                            for i = 1, 100 do
                                sum = sum + i
                            end
                            return scriptIndex * 1000 + sum
