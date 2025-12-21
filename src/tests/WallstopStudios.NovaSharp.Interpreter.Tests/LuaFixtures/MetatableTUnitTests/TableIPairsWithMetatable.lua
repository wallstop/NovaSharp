-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:45
-- @test: MetatableTUnitTests.TableIPairsWithMetatable
-- @compat-notes: Test targets Lua 5.2+
test = { 2, 4, 6 }
                meta = { }
                function meta.__ipairs(t)
                    local function ripairs_it(t,i)
                        i=i-1
                        local v=t[i]
                        if v==nil then return v end
                        return i,v
                    end
                    return ripairs_it, t, #t+1
                end
                setmetatable(test, meta);
                x = '';
                for i,v in ipairs(test) do
                    x = x .. i;
                end
                return x;
