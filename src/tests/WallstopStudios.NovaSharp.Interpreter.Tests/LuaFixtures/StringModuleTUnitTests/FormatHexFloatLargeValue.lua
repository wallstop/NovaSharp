-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatLargeValue
-- @compat-notes: %a format with value requiring significand
return string.format('%a', 255.0)
