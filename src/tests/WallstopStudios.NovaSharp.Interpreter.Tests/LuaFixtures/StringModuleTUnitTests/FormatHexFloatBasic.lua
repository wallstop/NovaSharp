-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatBasic
-- %a format is available in Lua 5.2+
return string.format('%a', 1.0)
