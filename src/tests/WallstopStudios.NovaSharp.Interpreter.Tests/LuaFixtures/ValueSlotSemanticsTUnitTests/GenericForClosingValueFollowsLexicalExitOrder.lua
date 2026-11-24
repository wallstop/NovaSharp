-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:186
-- @test: ValueSlotSemanticsTUnitTests.GenericForClosingValueFollowsLexicalExitOrder
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute; Lua 5.2+: goto statement (5.2+); Lua 5.2+: label (5.2+)
local log = {}
                local function closer(name)
                    return setmetatable({}, {__close = function() log[#log + 1] = name end})
                end
                local function once(_, previous)
                    if previous == 0 then return 1 end
                end

                do
                    local surrounding <close> = closer('surrounding')
                    for _ in once, nil, 0, closer('iterator') do
                        if true then
                            local inner <close> = closer('inner')
                            do break end
                        end
                    end
                    log[#log + 1] = 'after break'
                end

                for _ in once, nil, 0, closer('goto iterator') do
                    do
                        local inner <close> = closer('goto inner')
                        goto finished
                    end
                end
                ::finished::
                assert(table.concat(log, ',') == 'inner,iterator,after break,surrounding,goto inner,goto iterator')
