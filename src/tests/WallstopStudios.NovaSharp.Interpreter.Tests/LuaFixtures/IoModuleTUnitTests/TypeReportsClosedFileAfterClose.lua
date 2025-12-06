-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:96
-- @test: IoModuleTUnitTests.TypeReportsClosedFileAfterClose
-- @compat-notes: Lua 5.3+: bitwise operators
local f = io.open('{path}', 'w')
                local openType = io.Type(f)
                f:close()
                return openType, io.Type(f)
