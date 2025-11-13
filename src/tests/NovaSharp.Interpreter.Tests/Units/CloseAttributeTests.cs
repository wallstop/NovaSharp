namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public class CloseAttributeTests
    {
        [Test]
        public void ToBeClosedVariablesCloseInReverseOrderOnScopeExit()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local log = {}

                local function newcloser(name)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                        end
                    })
                    return token
                end

                local function run()
                    local first <close> = newcloser('first')
                    local second <close> = newcloser('second')
                end

                run()
                return log
                "
            );

            Assert.That(result.Type, Is.EqualTo(DataType.Table));
            Table log = result.Table;
            Assert.That(log.Length, Is.EqualTo(2));
            Assert.That(log.Get(1).String, Is.EqualTo("second:nil"));
            Assert.That(log.Get(2).String, Is.EqualTo("first:nil"));
        }

        [Test]
        public void ReassignmentClosesPreviousValueImmediately()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local log = {}

                local function newcloser(name)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                        end
                    })
                    return token
                end

                local function run()
                    local target <close> = newcloser('first')
                    target = newcloser('second')
                end

                run()
                return log
                "
            );

            Table log = result.Table;
            Assert.That(log.Length, Is.EqualTo(2));
            Assert.That(log.Get(1).String, Is.EqualTo("first:nil"), "old value should be closed before reassignment completes");
            Assert.That(log.Get(2).String, Is.EqualTo("second:nil"), "new value should close when the scope exits");
        }

        [Test]
        public void ErrorPathPassesErrorObjectToCloseMetamethod()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local captured = {}

                local function newcloser()
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            captured.err = err
                        end
                    })
                    return token
                end

                local function run()
                    local _ <close> = newcloser()
                    error('boom')
                end

                local ok, message = pcall(run)
                return ok, message, captured.err
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(3));
            Assert.That(result.Tuple[0].Boolean, Is.False);
            string errorMessage = result.Tuple[1].String;
            Assert.That(errorMessage, Does.Contain("boom"));
            string closeArgument = result.Tuple[2].String;
            Assert.That(closeArgument, Does.Contain("boom"), "error propagated to __close");
        }

        [Test]
        public void MissingCloseMetamethodRaisesRuntimeError()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function()
                    local _ <close> = {}
                end)
                return ok, err
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.That(result.Tuple[0].Boolean, Is.False);
            Assert.That(result.Tuple[1].String, Does.Contain("__close metamethod expected"));
        }

        [Test]
        public void GotoJumpOutOfScopeClosesLocals()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local log = {}

                local function newcloser(name)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                        end
                    })
                    return token
                end

                do
                    local outer <close> = newcloser('outer')
                    do
                        local inner <close> = newcloser('inner')
                        goto finish
                    end
                end

                ::finish::
                return log
                "
            );

            Table log = result.Table;
            Assert.That(log.Length, Is.EqualTo(2));
            Assert.That(log.Get(1).String, Is.EqualTo("inner:nil"));
            Assert.That(log.Get(2).String, Is.EqualTo("outer:nil"));
        }

        [Test]
        public void BreakStatementClosesLoopScopedLocals()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local log = {}

                local function newcloser(name)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                        end
                    })
                    return token
                end

                for i = 1, 3 do
                    local closer <close> = newcloser('loop_' .. i)
                    break
                end

                return log
                "
            );

            Table log = result.Table;
            Assert.That(log.Length, Is.EqualTo(1));
            Assert.That(log.Get(1).String, Is.EqualTo("loop_1:nil"));
        }
    }
}
