-- @lua-versions: 5.5
-- @novasharp-only: true
-- @expects-error: false
-- Uses injected variables: hostcall, hostclose
local log = {}

-- A host callback catches the internal abort and returns normally.
local co1
co1 = coroutine.create(function()
    local a <close> = setmetatable({}, {__close = function() log[#log + 1] = 'close1' end})
    hostcall(function() coroutine.close(co1) end)
    log[#log + 1] = 'unreachable1'
end)
local r1, v1 = coroutine.resume(co1)
local c1, ce1 = coroutine.close(co1)

-- Nested host callbacks both catch the abort.
local co2
co2 = coroutine.create(function()
    local a <close> = setmetatable({}, {__close = function() log[#log + 1] = 'close2' end})
    hostcall(function()
        hostcall(function() coroutine.close(co2) end)
        log[#log + 1] = 'unreachable2'
    end)
    log[#log + 1] = 'unreachable2b'
end)
local r2, v2 = coroutine.resume(co2)
local c2, ce2 = coroutine.close(co2)

-- A host close metamethod catches the abort raised by its nested call and cannot
-- resume Lua execution afterwards.
local co3
function closeSelf3() coroutine.close(co3) end
function afterClose3() log[#log + 1] = 'unreachable3' end
co3 = coroutine.create(function()
    do
        local a <close> = setmetatable({}, {__close = hostclose})
    end
    log[#log + 1] = 'unreachable3'
end)
local r3, v3 = coroutine.resume(co3)
local c3, ce3 = coroutine.close(co3)

return r1, v1, c1, ce1, r2, v2, c2, ce2, r3, v3, c3, ce3, table.concat(log, ','), co1, co2, co3
