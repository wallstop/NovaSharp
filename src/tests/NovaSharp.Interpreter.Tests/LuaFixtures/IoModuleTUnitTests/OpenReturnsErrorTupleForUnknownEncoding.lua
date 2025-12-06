-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:797
-- @test: IoModuleTUnitTests.OpenReturnsErrorTupleForUnknownEncoding
-- @compat-notes: Lua 5.3+: bitwise operators
local file, message = io.open('{escapedPath}', 'w', 'does-not-exist')
                return file, message
