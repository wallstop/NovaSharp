-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @compat-notes: Platform-specific: Windows strftime doesn't support POSIX %T specifier. NovaSharp implements POSIX-compliant behavior.
-- %T should return HH:MM:SS (24-hour time)
-- Epoch timestamp 0 is Thursday, January 1, 1970 00:00:00 UTC
return os.date("!%T", 0)
-- Expected: 00:00:00