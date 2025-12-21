-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/CloseAttributeTUnitTests.cs:156
-- @test: CloseAttributeTUnitTests.GotoJumpOutOfScopeClosesLocals
-- @compat-notes: Test targets Lua 5.4+; Lua 5.4: close attribute; Lua 5.2+: goto statement (5.2+); Lua 5.2+: label (5.2+); Uses injected variable: s
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
