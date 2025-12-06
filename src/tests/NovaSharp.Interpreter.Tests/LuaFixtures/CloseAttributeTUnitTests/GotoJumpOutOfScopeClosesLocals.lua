-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/CloseAttributeTUnitTests.cs:138
-- @test: CloseAttributeTUnitTests.GotoJumpOutOfScopeClosesLocals
-- @compat-notes: Lua 5.4: close attribute; Lua 5.4: goto statement; Lua 5.4: label; Lua 5.3+: bitwise operators
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

                do
                    local outer <close> = newcloser('outer')
                    do
                        local inner <close> = newcloser('inner')
                        goto finish
                    end
                end

                ::finish::
                return log
