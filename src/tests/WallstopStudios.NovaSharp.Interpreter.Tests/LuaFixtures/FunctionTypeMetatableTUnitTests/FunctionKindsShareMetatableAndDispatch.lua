-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/FunctionTypeMetatableTUnitTests.cs:15
-- @test: FunctionTypeMetatableTUnitTests.FunctionKindsShareMetatableAndDispatch

local function closure() return 'normal closure call' end
local native = type
local old = debug.getmetatable(closure)
assert(old == debug.getmetatable(native))
local backing = { marker = 'initial' }
local metadata = {
    __metatable = false,
    __index = backing,
    __newindex = backing,
    __tostring = function() return 'function hook' end,
    __add = function(left, right)
        if left == native then return 'native left' end
        if right == native then return 'native right' end
        return 'closure'
    end,
    __lt = function(left, right) return left == native and right == closure end,
    __le = function(left, right) return left == native and right == closure end,
    __eq = function() error('function equality must use identity') end,
    __call = function() error('ordinary function calls must bypass __call') end,
}
assert(debug.setmetatable(closure, metadata) == (_VERSION == 'Lua 5.1' and true or closure))
assert(debug.getmetatable(native) == metadata, 'a native function sees the metatable set on a closure')
assert(getmetatable(closure) == false and getmetatable(native) == false)
assert(not pcall(debug.setmetatable, native, true))
assert(debug.getmetatable(native) == metadata)

assert(closure.marker == 'initial' and native.marker == 'initial')
native.marker = 'native write'
assert(closure.marker == 'native write')
closure.marker = 'closure write'
assert(native.marker == 'closure write')
assert(tostring(closure) == 'function hook' and tostring(native) == 'function hook')
assert(native + 1 == 'native left' and 1 + native == 'native right')
assert(1 + closure == 'closure')
assert(native < closure and not (closure < native))
assert(native <= closure and not (closure <= native))
assert(native == native and closure ~= native)
assert(closure ~= io.stdout and io.stdout ~= native)
assert(native(closure) == 'function' and closure() == 'normal closure call')

local replacement = { __index = { marker = 'replacement' } }
assert(debug.setmetatable(native, replacement) == (_VERSION == 'Lua 5.1' and true or native))
assert(getmetatable(closure) == replacement and getmetatable(native) == replacement)
assert(closure.marker == 'replacement' and native.marker == 'replacement')
assert(not pcall(function() return native + 1 end))
assert(not pcall(function() return closure < native end))

assert(debug.setmetatable(native, nil) == (_VERSION == 'Lua 5.1' and true or native))
assert(debug.getmetatable(closure) == nil and debug.getmetatable(native) == nil)
assert(not pcall(function() return native.marker end))
debug.setmetatable(closure, old)
print('function metatable sharing, dispatch, and removal verified')
