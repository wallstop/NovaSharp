-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- Platform-specific: Windows strftime doesn't support POSIX %u specifier. NovaSharp implements POSIX-compliant behavior.
-- %u should return weekday number (1-7, Monday=1)
-- Epoch timestamp 0 is Thursday = 4
return os.date("!%u", 0)
-- Expected: 4