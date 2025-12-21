-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:398
-- @test: CoroutineModuleTUnitTests.ResumeFlattensNestedTupleResults
-- @compat-notes: Test targets Lua 5.2+
function returningTuple()
                    return 'tag', coroutine.running()
                end
