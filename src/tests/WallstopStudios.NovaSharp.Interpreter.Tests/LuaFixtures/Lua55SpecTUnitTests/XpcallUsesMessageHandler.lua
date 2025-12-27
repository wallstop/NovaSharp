-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:422
-- @test: Lua55SpecTUnitTests.XpcallUsesMessageHandler
-- @compat-notes: Test targets Lua 5.5+
local function bad() error('original') end
                local function handler(msg) return 'handled: ' .. tostring(msg) end
                local ok, msg = xpcall(bad, handler)
                return ok, string.find(msg, 'handled:') ~= nil
