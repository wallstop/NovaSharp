-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:79
-- @test: IoModuleTUnitTests.RegisteredFileCallbacksUseArgumentViews
assert(type(io.stderr.close) == 'function')
assert(type(io.stderr.flush) == 'function')
assert(type(io.stderr.lines) == 'function')
assert(type(io.stderr.read) == 'function')
assert(type(io.stderr.seek) == 'function')
assert(type(io.stderr.setvbuf) == 'function')
assert(type(io.stderr.write) == 'function')
print('PASS')
