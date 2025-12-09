-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\CloseAttributeTUnitTests.cs:175
-- @test: CloseAttributeTUnitTests.BreakStatementClosesLoopScopedLocals
-- @compat-notes: Lua 5.4: close attribute; Lua 5.3+: bitwise operators; Uses injected variable: s
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

                for i = 1, 3 do
                    local closer <close> = newcloser('loop_' .. i)
                    break
                end

                return log
