-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatLargeValue
-- @compat-notes: %a format with value requiring significand
return string.format('%a', 255.0)
