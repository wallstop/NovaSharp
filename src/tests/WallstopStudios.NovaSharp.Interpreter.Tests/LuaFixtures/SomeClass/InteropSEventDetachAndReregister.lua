-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\UserDataEventsTUnitTests.cs:304
-- @test: SomeClass.InteropSEventDetachAndReregister
-- @compat-notes: Uses injected variable: myobj
function handler(o, a)
                        ext();
                    end

                    myobj.MySEvent.add(handler);
