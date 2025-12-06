-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/CloseAttributeTUnitTests.cs:83
-- @test: CloseAttributeTUnitTests.ErrorPathPassesErrorObjectToCloseMetamethod
-- @compat-notes: Lua 5.4: close attribute; Lua 5.3+: bitwise operators
local captured = {}

                local function newcloser()
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            captured.err = err
                        end
                    })
                    return token
                end

                local function run()
                    local _ <close> = newcloser()
                    error('boom')
                end

                local ok, message = pcall(run)
                return ok, message, captured.err
