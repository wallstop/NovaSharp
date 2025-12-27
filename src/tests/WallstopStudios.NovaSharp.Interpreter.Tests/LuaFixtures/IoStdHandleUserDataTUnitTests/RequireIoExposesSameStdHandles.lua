-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:84
-- @test: IoStdHandleUserDataTUnitTests.RequireIoExposesSameStdHandles
local io_module = require('io')
                return io_module.stdin == io.stdin, io_module.stdout == io.stdout
