-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- Platform-specific: Windows strftime doesn't support POSIX %V specifier. NovaSharp implements POSIX-compliant behavior.
-- %V should return ISO week number (01-53)
-- Epoch timestamp 0 is Thursday, January 1, 1970 - ISO week 01
return os.date("!%V", 0)
-- Expected: 01