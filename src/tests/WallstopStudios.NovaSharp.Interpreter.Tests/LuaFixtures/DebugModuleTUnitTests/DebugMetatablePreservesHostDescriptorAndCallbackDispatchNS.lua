-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2167
-- @test: DebugModuleTUnitTests.DebugMetatablePreservesHostDescriptorAndCallbackDispatchNS

local thread = coroutine.create(function() end)
assert(thread == hostEquality and hostEquality == thread)
local mt = {
    __index = { threadOnly = true },
    __eq = function() error('userdata descriptor should handle the comparison') end,
}
debug.setmetatable(thread, mt)
assert(thread == hostEquality and hostEquality == thread)
local file = io.stdout
assert(type(file) == 'userdata' and type(file.write) == 'function')
assert(debug.getmetatable(file) == nil)
assert(not pcall(debug.setmetatable, file, {}))
assert(type(file.write) == 'function')
assert(type(print) == 'function')
assert(debug.getmetatable(print) == nil)
local function closure() end
local functionMeta = {
    __index = { callbackOnly = true },
    __eq = function() error('function equality must not intercept descriptor dispatch') end,
}
debug.setmetatable(print, functionMeta)
assert(debug.getmetatable(closure) == functionMeta and print.callbackOnly)
assert(thread == hostEquality and hostEquality == thread)
assert(closure ~= hostEquality and hostEquality ~= print)
assert(type(file.write) == 'function' and file.write.callbackOnly)
debug.setmetatable(closure, nil)
print('host dispatch verified')
