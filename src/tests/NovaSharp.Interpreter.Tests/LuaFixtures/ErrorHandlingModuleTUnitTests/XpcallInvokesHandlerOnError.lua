-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:233
-- @test: ErrorHandlingModuleTUnitTests.XpcallInvokesHandlerOnError
-- @compat-notes: Lua 5.3+: bitwise operators
local function handler(msg) return 'handled:' .. msg end
                local ok, err = xpcall(function() error('bad') end, handler)
                return ok, err
