-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:431
-- @test: CoroutineModuleTUnitTests.ResumeFlattensNestedTupleResultsLua51
-- @compat-notes: Test targets Lua 5.1
function returningTuple()
                    return 'tag', coroutine.running()
                end
