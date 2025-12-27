-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/CloseAttributeTUnitTests.cs:59
-- @test: CloseAttributeTUnitTests.ReassignmentClosesPreviousValueImmediately
-- @compat-notes: Test targets Lua 5.4+; Lua 5.4+: close attribute; Uses injected variable: s
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
                    local target <close> = newcloser('first')
                    target = newcloser('second')
                end

                run()
                return log
