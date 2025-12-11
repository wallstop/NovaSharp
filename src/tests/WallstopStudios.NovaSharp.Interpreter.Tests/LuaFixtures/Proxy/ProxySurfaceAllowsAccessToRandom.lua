-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\ProxyObjectsTUnitTests.cs:80
-- @test: Proxy.ProxySurfaceAllowsAccessToRandom
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: func
x = R.GetValue();
                    func(R);
