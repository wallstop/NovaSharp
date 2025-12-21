-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:422
-- @test: Lua55SpecTUnitTests.XpcallUsesMessageHandler
local function bad() error('original') end
                local function handler(msg) return 'handled: ' .. tostring(msg) end
                local ok, msg = xpcall(bad, handler)
                return ok, string.find(msg, 'handled:') ~= nil
