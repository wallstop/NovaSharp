-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:521
-- @test: CoroutineModuleTUnitTests.ResumeZeroValueYieldReturnsOnlyStatus
local co = coroutine.create(function()
                    coroutine.yield()
                end)

                return select('#', coroutine.resume(co))
