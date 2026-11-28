-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Compatibility notes: Lua 5.5 reports the first close-handler failure when a later closer self-closes.
local co
co = coroutine.create(function()
    local a <close> = setmetatable({}, {__close = function(_, e)
        coroutine.close(co)
    end})
    local b <close> = setmetatable({}, {__close = function(_, e)
        error('b-boom', 0)
    end})
    local c <close> = setmetatable({}, {__close = function(_, e)
        error('c-boom', 0)
    end})
end)
local resumed, resumeError = coroutine.resume(co)
local closed, closeError = coroutine.close(co)
print(resumed, tostring(resumeError), closed, tostring(closeError))
return resumed, tostring(resumeError), closed, tostring(closeError)
