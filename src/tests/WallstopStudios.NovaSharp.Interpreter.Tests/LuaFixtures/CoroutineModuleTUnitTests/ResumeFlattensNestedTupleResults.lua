-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\CoroutineModuleTUnitTests.cs:351
-- @test: CoroutineModuleTUnitTests.ResumeFlattensNestedTupleResults
-- @compat-notes: Test targets Lua 5.2+
function returningTuple()
                    return 'tag', coroutine.running()
                end
