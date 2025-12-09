-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:77
-- @test: IoStdHandleUserDataTUnitTests.RequireIoExposesSameStdHandles
-- @compat-notes: Lua 5.3+: bitwise operators
local io_module = require('io')
                return io_module.stdin == io.stdin, io_module.stdout == io.stdout
