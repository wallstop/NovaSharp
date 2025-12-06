-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:77
-- @test: IoStdHandleUserDataTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
local io_module = require('io')
                return io_module.stdin == io.stdin, io_module.stdout == io.stdout
