-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- Uses injected variable: decoratedBridge
local result = decoratedBridge()
assert(result == 'caught', 'host boundary recovery: ' .. tostring(result))
return result
