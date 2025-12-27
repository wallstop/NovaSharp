-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3456
-- @test: DebugModuleTUnitTests.HookMaskCharactersAreRecognized
-- @compat-notes: Test targets Lua 5.1
local results = {}
                local function hook() end
                
                -- Test each mask character individually
                debug.sethook(hook, 'c')
                local _, m1, _ = debug.gethook()
                results[1] = m1 == 'c'
                
                debug.sethook(hook, 'r')
                local _, m2, _ = debug.gethook()
                results[2] = m2 == 'r'
                
                debug.sethook(hook, 'l')
                local _, m3, _ = debug.gethook()
                results[3] = m3 == 'l'
                
                -- Test combined mask
                debug.sethook(hook, 'crl')
                local _, m4, _ = debug.gethook()
                results[4] = m4 == 'crl'
                
                debug.sethook()  -- Clear hook
                
                -- Use unpack for Lua 5.1 compatibility (table.unpack for 5.2+)
                local unpackFn = table.unpack or unpack
                return unpackFn(results)
