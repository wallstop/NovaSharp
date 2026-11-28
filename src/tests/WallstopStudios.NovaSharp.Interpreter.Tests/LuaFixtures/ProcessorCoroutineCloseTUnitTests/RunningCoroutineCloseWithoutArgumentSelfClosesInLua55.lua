-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 treats a missing coroutine.close argument as the running coroutine.
local log = {}
local co = coroutine.create(function()
    local a <close> = setmetatable({}, {__close = function(_, e)
        log[#log + 1] = 'a:' .. tostring(e)
    end})
    local ok, err = pcall(coroutine.close)
    log[#log + 1] = 'pcall:' .. tostring(ok) .. ':' .. tostring(err)
end)
local resumed, value = coroutine.resume(co)
local closed, closeError = coroutine.close(co)
print(resumed, tostring(value), closed, tostring(closeError), #log, table.concat(log, ','))
return resumed, tostring(value), closed, tostring(closeError), #log, table.concat(log, ',')
