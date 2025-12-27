-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ProxyObjectsTUnitTests.cs:83
-- @test: Proxy.ProxySurfaceAllowsAccessToRandom
-- @compat-notes: Uses injected variable: func
x = R.GetValue();
                    func(R);
