namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    internal sealed class CoroutineTests
    {
        [Test]
        public void CoroutineBasic()
        {
            string script =
                @"
				s = ''

				function foo()
					for i = 1, 4 do
						s = s .. i;
						coroutine.yield();
					end
				end

				function bar()
					for i = 5, 9 do
						s = s .. i;
						coroutine.yield();
					end
				end

				cf = coroutine.create(foo);
				cb = coroutine.create(bar);

				for i = 1, 4 do
					coroutine.resume(cf);
					s = s .. '-';
					coroutine.resume(cb);
					s = s .. ';';
				end

				return s;
				";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("1-5;2-6;3-7;4-8;"));
        }

        [Test]
        public void CoroutineWrap()
        {
            string script =
                @"
				s = ''

				function foo()
					for i = 1, 4 do
						s = s .. i;
						coroutine.yield();
					end
				end

				function bar()
					for i = 5, 9 do
						s = s .. i;
						coroutine.yield();
					end
				end

				cf = coroutine.wrap(foo);
				cb = coroutine.wrap(bar);

				for i = 1, 4 do
					cf();
					s = s .. '-';
					cb();
					s = s .. ';';
				end

				return s;
				";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("1-5;2-6;3-7;4-8;"));
        }

        [Test]
        public void CoroutineClrBoundaryHandling()
        {
            string code =
                @"
				function a()
					callback(b)
				end

				function b()
					coroutine.yield();
				end						

				c = coroutine.create(a);

				return coroutine.resume(c);		
				";

            // Load the code and get the returned function
            Script script = new()
            {
                Globals =
                {
                    ["callback"] = DynValue.NewCallback((ctx, args) => args[0].Function.Call()),
                },
            };

            DynValue ret = script.DoString(code);

            Assert.That(ret.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(ret.Tuple.Length, Is.EqualTo(2));
            Assert.That(ret.Tuple[0].Type, Is.EqualTo(DataType.Boolean));
            Assert.That(ret.Tuple[0].Boolean, Is.EqualTo(false));
            Assert.That(ret.Tuple[1].Type, Is.EqualTo(DataType.String));
            Assert.That(
                ret.Tuple[1].String.EndsWith("attempt to yield across a CLR-call boundary"),
                Is.True
            );
        }

        [Test]
        public void CoroutineVariousErrorHandling()
        {
            string last = "";
            string code =
                @"

function checkresume(step, ex, ey)
	local x, y = coroutine.resume(c)
	
	assert(x == ex, 'Step ' .. step .. ': ' .. tostring(ex) .. ' was expected, got ' .. tostring(x));
	assert(y:endsWith(ey), 'Step ' .. step .. ': ' .. tostring(ey) .. ' was expected, got ' .. tostring(y));
end


t = { }
m = { __tostring = function() print('2'); coroutine.yield(); print('3'); end }

setmetatable(t, m);


function a()
	checkresume(1, false, 'cannot resume non-suspended coroutine');
	coroutine.yield('ok');
	print(t);
	coroutine.yield('ok'); 
end

c = coroutine.create(a);

checkresume(2, true, 'ok');
checkresume(3, false, 'attempt to yield across a CLR-call boundary');
checkresume(4, false, 'cannot resume dead coroutine');
checkresume(5, false, 'cannot resume dead coroutine');
checkresume(6, false, 'cannot resume dead coroutine');

				";

            // Load the code and get the returned function
            Script script = new() { Options = { DebugPrint = (s) => last = s } };

            script.DoString(code);

            Assert.That("2", Is.EqualTo(last));
        }

        [Test]
        public void CoroutineDirectResume()
        {
            string code =
                @"
				return function()
					local x = 0
					while true do
						x = x + 1
						coroutine.yield(x)
						if (x > 5) then
							return 7
						end
					end
				end
				";

            // Load the code and get the returned function
            Script script = new();
            DynValue function = script.DoString(code);

            // Create the coroutine in C#
            DynValue coroutine = script.CreateCoroutine(function);

            // Loop the coroutine
            string ret = "";
            while (coroutine.Coroutine.State != CoroutineState.Dead)
            {
                DynValue x = coroutine.Coroutine.Resume();
                ret = ret + x.ToString();
            }

            Assert.That(ret, Is.EqualTo("1234567"));
        }

        [Test]
        public void CoroutineDirectAsEnumerable()
        {
            string code =
                @"
				return function()
					local x = 0
					while true do
						x = x + 1
						coroutine.yield(x)
						if (x > 5) then
							return 7
						end
					end
				end
				";

            // Load the code and get the returned function
            Script script = new();
            DynValue function = script.DoString(code);

            // Create the coroutine in C#
            DynValue coroutine = script.CreateCoroutine(function);

            // Loop the coroutine
            string ret = "";

            foreach (DynValue x in coroutine.Coroutine.AsTypedEnumerable())
            {
                ret = ret + x.ToString();
            }

            Assert.That(ret, Is.EqualTo("1234567"));
        }

        [Test]
        public void CoroutineAutoYield()
        {
            string code =
                @"
				function fib(n)
					if (n == 0 or n == 1) then
						return 1;
					else
						return fib(n - 1) + fib(n - 2);
					end
				end
				";

            // Load the code and get the returned function
            Script script = new(default);
            script.DoString(code);

            // get the function
            DynValue function = script.Globals.Get("fib");

            // Create the coroutine in C#
            DynValue coroutine = script.CreateCoroutine(function);

            // Set the automatic yield counter every 10 instructions.
            // 10 is a too small! Use a much bigger value in your code to avoid interrupting too often!
            coroutine.Coroutine.AutoYieldCounter = 10;

            int cycles = 0;
            DynValue result = null;

            // Cycle until we get that the coroutine has returned something useful and not an automatic yield..
            for (
                result = coroutine.Coroutine.Resume(8);
                result.Type == DataType.YieldRequest;
                result = coroutine.Coroutine.Resume()
            )
            {
                cycles += 1;
            }

            // Check the values of the operation
            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(34));

            // Check the autoyield actually triggered
            Assert.That(cycles > 10, Is.True);
        }
    }
}
