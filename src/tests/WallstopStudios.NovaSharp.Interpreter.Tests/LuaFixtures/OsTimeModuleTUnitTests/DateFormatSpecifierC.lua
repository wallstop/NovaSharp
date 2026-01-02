-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- @compat-notes: Platform-specific: Windows strftime doesn't support POSIX %C specifier. NovaSharp implements POSIX-compliant behavior.
-- %C should return century (00-99)
-- 1970 is in the 19th century (1900-1999)
return os.date("!%C", 0)
-- Expected: 19