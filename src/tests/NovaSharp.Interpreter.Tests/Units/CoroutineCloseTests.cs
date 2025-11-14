namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public class CoroutineCloseTests
    {
        [Test]
        public void ToBeClosedAllowsNilAssignments()
        {
            Script script = new();

            DynValue result = script.DoString(
                @"
                local function run()
                    local _ <close> = nil
                    return true
                end

                return run()
                "
            );

            Assert.That(result.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void ToBeClosedAllowsFalseAssignments()
        {
            Script script = new();

            DynValue result = script.DoString(
                @"
                local function run()
                    local _ <close> = false
                    return true
                end

                return run()
                "
            );

            Assert.That(result.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void CoroutineCloseFlushesPendingClosuresWhenSuspended()
        {
            Script script = new();

            DynValue result = script.DoString(
                @"
                local log = {}

                local function newcloser()
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, err or 'nil')
                        end
                    })
                    return token
                end

                local co = coroutine.create(function()
                    local guard <close> = newcloser()
                    coroutine.yield('pause')
                end)

                local okResume, resumeValue = coroutine.resume(co)
                local closeOk, closeErr = coroutine.close(co)

                return {
                    resumeOk = okResume,
                    resumeValue = resumeValue,
                    closeOk = closeOk,
                    closeErr = closeErr,
                    log = log
                }
                "
            );

            Table table = result.Table;
            Assert.That(table.Get("resumeOk").Boolean, Is.True);
            Assert.That(table.Get("resumeValue").String, Is.EqualTo("pause"));
            Assert.That(table.Get("closeOk").Boolean, Is.True);
            Assert.That(table.Get("closeErr").Type, Is.EqualTo(DataType.Nil));

            Table log = table.Get("log").Table;
            Assert.That(log.Length, Is.EqualTo(1));
            Assert.That(log.Get(1).String, Is.EqualTo("nil"));
        }

        [Test]
        public void CoroutineCloseReturnsOriginalErrorAfterFailure()
        {
            Script script = new();

            DynValue result = script.DoString(
                @"
                local log = {}

                local co = coroutine.create(function()
                    local guard <close> = setmetatable({}, {
                        __close = function(_, err)
                            table.insert(log, err or 'nil')
                        end
                    })
                    error('kapow')
                end)

                local resumeOk, resumeErr = coroutine.resume(co)
                local closeOk, closeErr = coroutine.close(co)
                local closeAgainOk, closeAgainErr = coroutine.close(co)

                return {
                    resumeOk = resumeOk,
                    resumeErr = resumeErr,
                    closeOk = closeOk,
                    closeErr = closeErr,
                    closeAgainOk = closeAgainOk,
                    closeAgainErr = closeAgainErr,
                    log = log
                }
                "
            );

            Table table = result.Table;
            Assert.That(table.Get("resumeOk").Boolean, Is.False);
            Assert.That(table.Get("resumeErr").String, Does.Contain("kapow"));
            Assert.That(table.Get("closeOk").Boolean, Is.False);
            Assert.That(table.Get("closeErr").String, Does.Contain("kapow"));
            Assert.That(table.Get("closeAgainOk").Boolean, Is.False);
            Assert.That(table.Get("closeAgainErr").String, Does.Contain("kapow"));

            Table log = table.Get("log").Table;
            Assert.That(log.Length, Is.EqualTo(1));
            Assert.That(log.Get(1).String, Does.Contain("kapow"));
        }
    }
}
