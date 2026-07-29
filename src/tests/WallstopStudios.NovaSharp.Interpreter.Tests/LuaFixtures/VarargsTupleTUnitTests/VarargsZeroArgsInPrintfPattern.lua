-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\VarargsTupleTUnitTests.cs:219
-- @test: VarargsTupleTUnitTests.VarargsZeroArgsInPrintfPattern
function printf(fmt, ...)
                    if select('#', ...) == 0 then
                        return 'no_extra_args'
                    else
                        return 'has_extra_args'
                    end
                end
                return printf('hello')
