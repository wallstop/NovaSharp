-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatBasic
-- @compat-notes: %a format is available in Lua 5.2+
return string.format('%a', 1.0)
