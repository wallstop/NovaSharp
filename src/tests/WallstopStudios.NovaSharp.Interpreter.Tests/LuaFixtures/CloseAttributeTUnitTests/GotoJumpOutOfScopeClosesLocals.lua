-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\CloseAttributeTUnitTests.cs:138
-- @test: CloseAttributeTUnitTests.GotoJumpOutOfScopeClosesLocals
-- @compat-notes: Lua 5.4: close attribute; Lua 5.4: goto statement; Lua 5.4: label; Lua 5.3+: bitwise operators; Uses injected variable: s
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
