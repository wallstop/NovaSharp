-- @lua-versions: 5.2+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs
-- @test: OsTimeModuleTUnitTests.DateSupportsOyModifierInLua52Plus
-- Platform-specific: Windows strftime doesn't support POSIX %Oy specifier. NovaSharp implements POSIX-compliant behavior.

-- Reference: lua5.2 -e "print(os.date('!%Oy', 0))" outputs "70"
-- %Oy is a valid POSIX O modifier combination
-- In C locale, %Oy outputs the same as %y (2-digit year)
local result = os.date('!%Oy', 0)
assert(result == "70", "Expected '70', got: " .. tostring(result))
return result