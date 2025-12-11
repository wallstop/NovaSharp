-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\JsonModuleTUnitTests.cs:90
-- @test: JsonModuleTUnitTests.IsNullDetectsJsonNullAndNil
-- @compat-notes: Test class 'JsonModuleTUnitTests' uses NovaSharp-specific JsonModule functionality
local json = require('json')
                return json.isnull(json.null()),
                       json.isnull(nil),
                       json.isnull(false),
                       json.isnull({})
