-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:196
-- @test: TableTUnitTests.LoadReturnsSyntaxError
function reader()
                    i = i + 1
                    return t[i]
                end
                t = { [[?syntax error?]] }
                i = 0
                f, msg = load(reader, 'errorchunk')
                return f, msg
