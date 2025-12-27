-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3393
-- @test: DebugModuleTUnitTests.GetInfoAllFieldsHaveExpectedTypes
-- @compat-notes: Test targets Lua 5.1
local function sample(a, b)
                    local c = a + b
                    return c
                end
                local info = debug.getinfo(sample, 'nSluf')
                
                -- Check field types (return type names or nil if field missing)
                return 
                    type(info.name) == 'string' or info.name == nil,  -- name can be nil for anonymous
                    type(info.what) == 'string',
                    type(info.source) == 'string',
                    type(info.short_src) == 'string',
                    type(info.linedefined) == 'number',
                    type(info.lastlinedefined) == 'number',
                    type(info.nups) == 'number',
                    type(info.nparams) == 'number' or info.nparams == nil,  -- Lua 5.2+
                    type(info.isvararg) == 'boolean' or info.isvararg == nil  -- Lua 5.2+
