-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataEventsTUnitTests.cs:67
-- @test: SomeClass.InteropEventSimple
-- @compat-notes: Uses injected variable: myobj
function handler(o, a)
                        ext();
                    end

                    myobj.MyEvent.add(handler);
