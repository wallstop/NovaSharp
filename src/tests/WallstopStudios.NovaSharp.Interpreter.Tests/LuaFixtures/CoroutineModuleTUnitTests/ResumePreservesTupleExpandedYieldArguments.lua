-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:741
-- @test: CoroutineModuleTUnitTests.ResumePreservesTupleExpandedYieldArguments
function buildYieldValues()
                    return 'expanded-a', 'expanded-b'
                end

                function yieldExpanded()
                    coroutine.yield('head', buildYieldValues())
                end
