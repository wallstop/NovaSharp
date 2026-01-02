-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- @compat-notes: Platform-specific: Windows strftime doesn't support POSIX %n specifier. NovaSharp implements POSIX-compliant behavior.
-- %n should return a newline character
return os.date("!%n", 0) == "\n"
-- Expected: true