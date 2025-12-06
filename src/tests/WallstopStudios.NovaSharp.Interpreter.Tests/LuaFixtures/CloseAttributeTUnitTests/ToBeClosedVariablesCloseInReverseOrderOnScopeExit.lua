-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/CloseAttributeTUnitTests.cs:14
-- @test: CloseAttributeTUnitTests.ToBeClosedVariablesCloseInReverseOrderOnScopeExit
-- @compat-notes: Lua 5.4: close attribute; Lua 5.3+: bitwise operators
local log = {}

                local function newcloser(name)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                        end
                    })
                    return token
                end

                local function run()
                    local first <close> = newcloser('first')
                    local second <close> = newcloser('second')
                end

                run()
                return log
