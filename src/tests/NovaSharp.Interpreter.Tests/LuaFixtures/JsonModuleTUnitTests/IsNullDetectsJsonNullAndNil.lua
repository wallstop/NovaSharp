-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:90
-- @test: JsonModuleTUnitTests.IsNullDetectsJsonNullAndNil
-- @compat-notes: Lua 5.3+: bitwise operators
local json = require('json')
                return json.isnull(json.null()),
                       json.isnull(nil),
                       json.isnull(false),
                       json.isnull({})
