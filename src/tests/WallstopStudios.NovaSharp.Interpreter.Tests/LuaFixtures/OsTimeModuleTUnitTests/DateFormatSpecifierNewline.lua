-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @compat-notes: Platform-specific: Windows strftime doesn't support POSIX %n specifier. NovaSharp implements POSIX-compliant behavior.
-- %n should return a newline character
return os.date("!%n", 0) == "\n"
-- Expected: true