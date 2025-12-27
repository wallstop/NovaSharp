-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:237
-- @test: SetFenvGetFenvTUnitTests.SetFenvWithNonTableSecondArgThrows
-- @compat-notes: Test targets Lua 5.1
local f = function() end; setfenv(f, 'string')
