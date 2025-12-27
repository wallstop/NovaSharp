-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataEventsTUnitTests.cs:173
-- @test: SomeClass.InteropEventMultiAndDetach
-- @compat-notes: Uses injected variable: myobj
local invocationCount = 0
                    function handler(o, a)
                        invocationCount = invocationCount + 1;
                    end

                    myobj.MyEvent.add(handler);
                    myobj.MyEvent.add(handler);
                    myobj.TriggerMyEvent();
                    myobj.MyEvent.remove(handler);
                    myobj.TriggerMyEvent();
                    return invocationCount;
