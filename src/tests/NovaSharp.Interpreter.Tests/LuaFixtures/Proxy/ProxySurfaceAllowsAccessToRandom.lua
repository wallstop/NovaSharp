-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ProxyObjectsTUnitTests.cs:80
-- @test: Proxy.ProxySurfaceAllowsAccessToRandom
-- @compat-notes: Lua 5.3+: bitwise operators
x = R.GetValue();
                    func(R);
