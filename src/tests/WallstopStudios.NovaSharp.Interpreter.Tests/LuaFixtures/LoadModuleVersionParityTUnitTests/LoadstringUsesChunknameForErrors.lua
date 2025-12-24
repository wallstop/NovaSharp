-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:72
-- @test: LoadModuleVersionParityTUnitTests.LoadstringUsesChunknameForErrors
-- @compat-notes: Test targets Lua 5.1
local f, err = loadstring('error("boom")', 'my-chunk')
                assert(f ~= nil, 'loadstring should return a function')
                local ok, msg = pcall(f)
                return msg
