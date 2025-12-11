-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\BinaryDumpTUnitTests.cs:202
-- @test: BinaryDumpTUnitTests.LoadChangeEnvWithDebugSetUpValue
-- @compat-notes: NovaSharp: potential NovaSharp sandbox
function print_env()
                    print(_ENV)
                end

                function sandbox()
                    print(_ENV)
                    _ENV = { print = print, print_env = print_env, debug = debug, load = load }
                    print(_ENV)
                    print_env()
                    local code1 = load('print(_ENV)')
                    code1()
                    debug.setupvalue(code1, 0, _ENV)
                    debug.setupvalue(code1, 1, _ENV)
                    code1()
                    local code2 = load('print(_ENV)', nil, nil, _ENV)
                    code2()
                end

                sandbox()
