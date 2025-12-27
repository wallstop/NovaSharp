-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataEventsTUnitTests.cs:145
-- @test: SomeClass.InteropEventMulti
-- @compat-notes: Uses injected variable: myobj
function handler(o, a)
                        ext();
                    end

                    myobj.MyEvent.add(handler);
                    myobj.MyEvent.add(handler);
