-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:246
-- @test: ErrorHandlingModuleTUnitTests.XpcallPassesErrorObjectsToHandlerAndUsesItsResult
local original = {}
                local seenIdentity = false
                local ok, caught = xpcall(function() error(original) end, function(value)
                    seenIdentity = rawequal(value, original)
                    return value
                end)

                local replacement = {}
                local ok2, caught2 = xpcall(function() error('boom') end, function() return replacement end)

                local nilSeen = 'sentinel'
                local ok3, caught3 = xpcall(function() error(nil) end, function(value)
                    nilSeen = value
                end)

                print('table', ok, rawequal(original, caught), seenIdentity, type(caught))
                print('replace', ok2, rawequal(replacement, caught2), type(caught2))
                print('nil', ok3, nilSeen == nil, type(caught3), caught3)
                return
                    ok, rawequal(original, caught), seenIdentity, type(caught),
                    ok2, rawequal(replacement, caught2), type(caught2),
                    ok3, nilSeen == nil, type(caught3), caught3
