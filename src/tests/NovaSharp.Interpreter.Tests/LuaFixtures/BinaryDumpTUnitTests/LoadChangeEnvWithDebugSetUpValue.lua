-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/BinaryDumpTUnitTests.cs:202
-- @test: BinaryDumpTUnitTests.LoadChangeEnvWithDebugSetUpValue
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.2+: _ENV variable
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
