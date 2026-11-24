-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:380
-- @test: ValueSlotSemanticsTUnitTests.GenericForCloseCompletesNativeTailRequests
for _, chained in ipairs({false, true}) do
                    chainedCloser = chained
                    tailContinuation = nil
                    local callable = chained and setmetatable({}, {__call = tailCloser})
                    local resource = setmetatable({}, {__close = callable or tailCloser})
                    local thread = coroutine.create(function()
                        for _ in function() end, nil, nil, resource do end
                    end)
                    local ok, err = coroutine.resume(thread)
                    assert(ok, err)
                    assert(tailThread == thread and tailSelf == (callable or nil))
                    assert(tailResource == resource and tailError == nil)
                    assert(tailContinuation == 17)
                end
