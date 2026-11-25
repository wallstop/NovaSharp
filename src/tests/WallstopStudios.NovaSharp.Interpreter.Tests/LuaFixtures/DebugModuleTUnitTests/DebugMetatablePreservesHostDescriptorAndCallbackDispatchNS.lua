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
assert(not pcall(debug.setmetatable, print, {}))
print('host dispatch verified')
