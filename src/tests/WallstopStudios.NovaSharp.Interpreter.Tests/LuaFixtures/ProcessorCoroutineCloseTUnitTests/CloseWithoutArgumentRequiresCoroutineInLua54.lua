-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.4 requires an explicit coroutine argument for coroutine.close.
local log = {}
local co = coroutine.create(function()
    local a <close> = setmetatable({}, {__close = function(_, e)
        log[#log + 1] = 'a:' .. tostring(e)
    end})
    local ok, err = pcall(coroutine.close)
    local rejected = not ok
        and type(err) == 'string'
        and err:find('close', 1, true) ~= nil
    log[#log + 1] = 'pcall:' .. tostring(rejected)
end)
local resumed, value = coroutine.resume(co)
local closed, closeError = coroutine.close(co)
print(resumed, tostring(value), closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(value), closed, tostring(closeError), #log, table.concat(log, ',')
