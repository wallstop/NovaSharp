-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatNegative
-- @compat-notes: %a format with negative value
return string.format('%a', -1.5)
