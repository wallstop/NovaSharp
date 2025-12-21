-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:226
-- @test: MyObject.MetatableExtensibleObjectSample
extensibleObjectMeta = {
                    __index = function(t, name)
                        local obj = rawget(t, 'wrappedobj');
                        if (obj) then return obj[name]; end
                    end
                }
                myobj = { wrappedobj = o };
                setmetatable(myobj, extensibleObjectMeta);
                function myobj.extended()
                    return 12;
                end
                return myobj.extended() * myobj.getSomething();
