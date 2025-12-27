-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatNegative
-- @compat-notes: %a format with negative value
return string.format('%a', -1.5)
