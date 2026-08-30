-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:169
-- @test: IoModuleTUnitTests.FileArgumentViewCallbacksNormalizeMethodReceiverOnce
-- Detached userdata callbacks are a NovaSharp interop extension.
local file = assert(io.tmpfile())
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
return detached, colon
