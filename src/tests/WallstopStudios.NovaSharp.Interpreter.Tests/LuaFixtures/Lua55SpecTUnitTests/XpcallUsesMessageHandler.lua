-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:390
-- @test: Lua55SpecTUnitTests.XpcallUsesMessageHandler
-- @compat-notes: Lua 5.3+: bitwise operators
local function bad() error('original') end
                local function handler(msg) return 'handled: ' .. tostring(msg) end
                local ok, msg = xpcall(bad, handler)
                return ok, string.find(msg, 'handled:') ~= nil
