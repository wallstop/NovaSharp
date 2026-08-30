-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:168
-- @test: IoModuleTUnitTests.FileArgumentViewCallbacksNormalizeMethodReceiverOnce
-- NovaSharp binds userdata member callbacks to their host object, so detached invocation is an interop extension.
local path = os.tmpname()
local file = assert(io.open(path, 'w+'))
local write = file.write
local seek = file.seek
local read = file.read
local flush = file.flush
local close = file.close

write('alpha\n')
assert(flush() == true)
assert(seek('set', 0) == 0)
local detached = read('*l')
assert(seek('set', 0) == 0)
local colon = file:read('*l')
assert(close() == true)
os.remove(path)
return detached, colon
