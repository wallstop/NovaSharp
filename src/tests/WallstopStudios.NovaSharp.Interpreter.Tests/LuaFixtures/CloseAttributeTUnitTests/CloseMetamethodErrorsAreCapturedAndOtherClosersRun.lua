-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/CloseAttributeTUnitTests.cs:231
-- @test: CloseAttributeTUnitTests.CloseMetamethodErrorsAreCapturedAndOtherClosersRun
-- @compat-notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local log = {}

                local function newcloser(name, should_error)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                            if should_error then
                                error('close:' .. name, 0)
                            end
                        end
                    })
                    return token
                end

                local function run()
                    local first <close> = newcloser('first', true)
                    local second <close> = newcloser('second', false)
                end

                local ok, err = pcall(run)
                return ok, err, log
