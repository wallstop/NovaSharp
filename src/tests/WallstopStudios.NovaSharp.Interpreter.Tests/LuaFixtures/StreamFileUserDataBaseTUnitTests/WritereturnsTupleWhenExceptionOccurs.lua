-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:60
-- @test: StreamFileUserDataBaseTUnitTests.WritereturnsTupleWhenExceptionOccurs
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: file
local f = file
                return f:write('boom')
